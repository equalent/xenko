// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.VirtualReality
{
    internal static class NativeInvoke
    {
#if SILICONSTUDIO_PLATFORM_IOS
        internal const string Library = "__Internal";
#else
        internal const string Library = "libxenkovr";
#endif

        internal static void PreLoad()
        {
#if SILICONSTUDIO_PLATFORM_WINDOWS
            NativeLibrary.PreloadLibrary(Library + ".dll");
#else
            NativeLibrary.PreloadLibrary(Library + ".so");
#endif
        }

        static NativeInvoke()
        {
            PreLoad();
        }
    }
}
