﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;

namespace ShellUtility.Windows.Utility;

static class HookUtility
{

    #region Pinvoke

    delegate void WinEventDelegate(IntPtr hWinEventHook, Event eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    public enum Event : uint
    {
        OBJECT_CREATE = 0x8000,
        OBJECT_DESTROY = 0x8001,
        SYSTEM_FOREGROUND = 0x0003,
        OBJECT_NAMECHANGE = 0x800C,
        OBJECT_PARENTCHANGE = 0x800F,
        SYSTEM_MOVESIZESTART = 0x000A,
        SYSTEM_MOVESIZEEND = 0x000B
    }

    const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    #endregion

    readonly static WinEventDelegate dele;
    static IntPtr hookHandle;

    static HookUtility()
    {
        dele = new WinEventDelegate(Callback);
        hookHandle = SetWinEventHook((uint)Event.OBJECT_CREATE, (uint)Event.SYSTEM_MOVESIZEEND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    static void WindowCreated(object sender, AutomationEventArgs e)
    {
        if (sender is AutomationElement element &&
            element?.Current.NativeWindowHandle is int handle &&
            genericCallbacks.ContainsKey(Event.OBJECT_CREATE))
            foreach (var callback in genericCallbacks[Event.OBJECT_CREATE])
                callback?.Invoke((IntPtr)handle);
    }

    static void Callback(IntPtr _0, Event @event, IntPtr hwnd, int _1, int _2, uint _3, uint _4)
    {
        Application.Current?.Dispatcher.InvokeAsync(() => Callbacks(@event, hwnd));
    }

    #region Callbacks

    static void Callbacks(Event @event, IntPtr handle)
    {

        if (genericCallbacks.TryGetValue(@event, out var callbacks))
            foreach (var callback in callbacks.ToArray())
                callback?.Invoke(handle);

        if (HookUtility.windowCallbacks.TryGetValue(@event, out var windowCallbacks))
            if (windowCallbacks.TryGetValue(handle, out var callbacks2))
                foreach (var callback in callbacks2.ToArray())
                    callback?.Invoke();

    }

    static readonly Dictionary<Event, List<Action<IntPtr>>> genericCallbacks = new Dictionary<Event, List<Action<IntPtr>>>();
    static readonly Dictionary<Event, Dictionary<IntPtr, List<Action>>> windowCallbacks = new Dictionary<Event, Dictionary<IntPtr, List<Action>>>();

    public static void RemoveHook(Event @event, Action<IntPtr> action) =>
        AddHook(@event, action, active: false);

    public static void RemoveHook(Event @event, IntPtr window, Action action) =>
        AddHook(@event, window, action, active: false);

    public static void AddHook(Event @event, Action<IntPtr> action, bool active = true)
    {

        if (!genericCallbacks.TryGetValue(@event, out var callbacks))
            genericCallbacks.Add(@event, callbacks = new List<Action<IntPtr>>());

        if (active && !callbacks.Contains(action))
            callbacks.Add(action);
        else if (!active && callbacks.Contains(action))
            _ = callbacks.Remove(action);

    }

    public static void AddHook(Event @event, IntPtr window, Action action, bool active = true)
    {

        if (!HookUtility.windowCallbacks.TryGetValue(@event, out var windowCallbacks))
            HookUtility.windowCallbacks.Add(@event, windowCallbacks = new Dictionary<IntPtr, List<Action>>());

        if (!windowCallbacks.TryGetValue(window, out var callbacks))
            windowCallbacks.Add(window, callbacks = new List<Action>());

        if (active && !callbacks.Contains(action))
            callbacks.Add(action);
        else if (!active && callbacks.Contains(action))
            _ = callbacks.Remove(action);

    }

    #endregion

}
