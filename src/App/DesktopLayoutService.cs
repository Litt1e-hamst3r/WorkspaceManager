using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Windows;

namespace WorkspaceManager.App;

public sealed class DesktopLayoutService
{
    private const int LvmFirst = 0x1000;
    private const int LvmGetItemCount = LvmFirst + 4;
    private const int LvmGetItemPosition = LvmFirst + 16;
    private const int LvmSetItemPosition = LvmFirst + 15;
    private const int LvmGetItemTextW = LvmFirst + 115;
    private const uint LvifText = 0x0001;
    private const uint MemCommit = 0x1000;
    private const uint MemReserve = 0x2000;
    private const uint MemRelease = 0x8000;
    private const uint PageReadWrite = 0x04;

    private readonly DesktopLayoutStore _layoutStore;

    public DesktopLayoutService(DesktopLayoutStore layoutStore)
    {
        _layoutStore = layoutStore;
    }

    public IReadOnlyList<DesktopLayoutSnapshot> GetSavedLayouts()
    {
        return _layoutStore.GetAll();
    }

    public DesktopLayoutSnapshot Capture(string? name = null)
    {
        var listViewHandle = FindDesktopListView();
        if (listViewHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法定位桌面图标列表。");
        }

        var items = ReadDesktopItems(listViewHandle);
        if (items.Count == 0)
        {
            throw new InvalidOperationException("当前未读取到任何桌面图标。");
        }

        return new DesktopLayoutSnapshot
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name)
                ? $"布局 {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : name.Trim(),
            ResolutionWidth = (int)SystemParameters.PrimaryScreenWidth,
            ResolutionHeight = (int)SystemParameters.PrimaryScreenHeight,
            CreatedAt = DateTimeOffset.Now,
            Items = items
        };
    }

    public void Save(DesktopLayoutSnapshot snapshot)
    {
        _layoutStore.Save(snapshot);
    }

    public void Restore(string id)
    {
        var snapshot = _layoutStore.Load(id)
            ?? throw new InvalidOperationException("未找到指定布局。");

        var listViewHandle = FindDesktopListView();
        if (listViewHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法定位桌面图标列表。");
        }

        var currentItems = ReadDesktopItemDescriptors(listViewHandle);
        var snapshotMap = snapshot.Items.ToDictionary(item => item.DisplayName, StringComparer.OrdinalIgnoreCase);

        foreach (var currentItem in currentItems)
        {
            if (!snapshotMap.TryGetValue(currentItem.DisplayName, out var savedItem))
            {
                continue;
            }

            NativeMethods.SendMessage(
                listViewHandle,
                LvmSetItemPosition,
                (IntPtr)currentItem.Index,
                MakeLParam(savedItem.PositionX, savedItem.PositionY));
        }
    }

    public void Delete(string id)
    {
        _layoutStore.Delete(id);
    }

    private static List<DesktopLayoutItem> ReadDesktopItems(IntPtr listViewHandle)
    {
        return ReadDesktopItemDescriptors(listViewHandle)
            .Select(descriptor => new DesktopLayoutItem
            {
                DisplayName = descriptor.DisplayName,
                PositionX = descriptor.PositionX,
                PositionY = descriptor.PositionY
            })
            .ToList();
    }

    private static List<DesktopItemDescriptor> ReadDesktopItemDescriptors(IntPtr listViewHandle)
    {
        NativeMethods.GetWindowThreadProcessId(listViewHandle, out var processId);
        if (processId == 0)
        {
            throw new InvalidOperationException("无法定位 Explorer 进程。");
        }

        using var processHandle = NativeMethods.OpenProcess(
            ProcessAccessFlags.QueryInformation |
            ProcessAccessFlags.VirtualMemoryOperation |
            ProcessAccessFlags.VirtualMemoryRead |
            ProcessAccessFlags.VirtualMemoryWrite,
            false,
            processId);

        if (processHandle.IsInvalid)
        {
            throw new InvalidOperationException("无法打开 Explorer 进程。");
        }

        var itemCount = NativeMethods.SendMessage(listViewHandle, LvmGetItemCount, IntPtr.Zero, IntPtr.Zero).ToInt32();
        if (itemCount <= 0)
        {
            return [];
        }

        var pointSize = Marshal.SizeOf<NativePoint>();
        var textBufferLength = 260;
        var textBufferSize = textBufferLength * sizeof(char);
        var lvItemSize = Marshal.SizeOf<NativeLvItem>();

        using var remotePointBuffer = RemoteBuffer.Allocate(processHandle, pointSize);
        using var remoteTextBuffer = RemoteBuffer.Allocate(processHandle, textBufferSize);
        using var remoteLvItemBuffer = RemoteBuffer.Allocate(processHandle, lvItemSize);

        var results = new List<DesktopItemDescriptor>(itemCount);
        for (var index = 0; index < itemCount; index++)
        {
            var displayName = ReadItemText(listViewHandle, processHandle, remoteLvItemBuffer, remoteTextBuffer, index, textBufferLength);
            var position = ReadItemPosition(listViewHandle, processHandle, remotePointBuffer, index);
            results.Add(new DesktopItemDescriptor(index, displayName, position.X, position.Y));
        }

        return results;
    }

    private static string ReadItemText(
        IntPtr listViewHandle,
        SafeProcessHandle processHandle,
        RemoteBuffer remoteLvItemBuffer,
        RemoteBuffer remoteTextBuffer,
        int index,
        int textBufferLength)
    {
        var lvItem = new NativeLvItem
        {
            mask = LvifText,
            iItem = index,
            iSubItem = 0,
            pszText = remoteTextBuffer.Pointer,
            cchTextMax = textBufferLength
        };

        var lvItemBytes = StructureToBytes(lvItem);
        NativeMethods.WriteProcessMemory(processHandle, remoteLvItemBuffer.Pointer, lvItemBytes, lvItemBytes.Length, out _);
        NativeMethods.SendMessage(listViewHandle, LvmGetItemTextW, (IntPtr)index, remoteLvItemBuffer.Pointer);

        var textBytes = new byte[textBufferLength * sizeof(char)];
        NativeMethods.ReadProcessMemory(processHandle, remoteTextBuffer.Pointer, textBytes, textBytes.Length, out _);

        var value = Encoding.Unicode.GetString(textBytes);
        var zeroIndex = value.IndexOf('\0');
        return zeroIndex >= 0 ? value[..zeroIndex] : value.Trim();
    }

    private static NativePoint ReadItemPosition(
        IntPtr listViewHandle,
        SafeProcessHandle processHandle,
        RemoteBuffer remotePointBuffer,
        int index)
    {
        NativeMethods.SendMessage(listViewHandle, LvmGetItemPosition, (IntPtr)index, remotePointBuffer.Pointer);
        var buffer = new byte[Marshal.SizeOf<NativePoint>()];
        NativeMethods.ReadProcessMemory(processHandle, remotePointBuffer.Pointer, buffer, buffer.Length, out _);
        return BytesToStructure<NativePoint>(buffer);
    }

    private static IntPtr FindDesktopListView()
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        var shellView = FindShellView(progman);
        if (shellView != IntPtr.Zero)
        {
            return NativeMethods.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", "FolderView");
        }

        var worker = IntPtr.Zero;
        while (true)
        {
            worker = NativeMethods.FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
            if (worker == IntPtr.Zero)
            {
                break;
            }

            shellView = FindShellView(worker);
            if (shellView == IntPtr.Zero)
            {
                continue;
            }

            var folderView = NativeMethods.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", "FolderView");
            if (folderView != IntPtr.Zero)
            {
                return folderView;
            }
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindShellView(IntPtr parentHandle)
    {
        return parentHandle == IntPtr.Zero
            ? IntPtr.Zero
            : NativeMethods.FindWindowEx(parentHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
    }

    private static IntPtr MakeLParam(int low, int high)
    {
        return (IntPtr)(((high & 0xFFFF) << 16) | (low & 0xFFFF));
    }

    private static byte[] StructureToBytes<T>(T value) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var buffer = new byte[size];
        var pointer = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(value, pointer, false);
            Marshal.Copy(pointer, buffer, 0, size);
            return buffer;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static T BytesToStructure<T>(byte[] buffer) where T : struct
    {
        var pointer = Marshal.AllocHGlobal(buffer.Length);
        try
        {
            Marshal.Copy(buffer, 0, pointer, buffer.Length);
            return Marshal.PtrToStructure<T>(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private sealed record DesktopItemDescriptor(int Index, string DisplayName, int PositionX, int PositionY);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeLvItem
    {
        public uint mask;
        public int iItem;
        public int iSubItem;
        public uint state;
        public uint stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
        public int iIndent;
        public int iGroupId;
        public uint cColumns;
        public IntPtr puColumns;
        public IntPtr piColFmt;
        public int iGroup;
    }

    [Flags]
    private enum ProcessAccessFlags : uint
    {
        QueryInformation = 0x0400,
        VirtualMemoryOperation = 0x0008,
        VirtualMemoryRead = 0x0010,
        VirtualMemoryWrite = 0x0020
    }

    private sealed class RemoteBuffer : IDisposable
    {
        private readonly SafeProcessHandle _processHandle;

        private RemoteBuffer(SafeProcessHandle processHandle, IntPtr pointer)
        {
            _processHandle = processHandle;
            Pointer = pointer;
        }

        public IntPtr Pointer { get; }

        public static RemoteBuffer Allocate(SafeProcessHandle processHandle, int size)
        {
            var pointer = NativeMethods.VirtualAllocEx(
                processHandle,
                IntPtr.Zero,
                (nuint)size,
                MemCommit | MemReserve,
                PageReadWrite);

            if (pointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法为 Explorer 进程分配内存。");
            }

            return new RemoteBuffer(processHandle, pointer);
        }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero)
            {
                NativeMethods.VirtualFreeEx(_processHandle, Pointer, nuint.Zero, MemRelease);
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string? className, string? windowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(ProcessAccessFlags access, bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(SafeProcessHandle processHandle, IntPtr address, nuint size, uint allocationType, uint protect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualFreeEx(SafeProcessHandle processHandle, IntPtr address, nuint size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(SafeProcessHandle processHandle, IntPtr baseAddress, [Out] byte[] buffer, int size, out IntPtr numberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(SafeProcessHandle processHandle, IntPtr baseAddress, byte[] buffer, int size, out IntPtr numberOfBytesWritten);
    }
}
