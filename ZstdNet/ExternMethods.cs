using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using size_t = System.UIntPtr;

namespace ZstdNet
{
	internal static class ExternMethods
	{
		static ExternMethods()
		{
			if(Environment.OSVersion.Platform == PlatformID.Win32NT)
				SetWinDllDirectory();
		}

		private static void SetWinDllDirectory()
		{
		    var locations = new[] {Assembly.GetExecutingAssembly().Location, new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath};
			var processedLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var location in locations) {
				if (processedLocations.Contains(location)) continue;

				string path;

				if(string.IsNullOrEmpty(location) || (path = Path.GetDirectoryName(location)) == null)
				{
					Trace.TraceWarning($"{nameof(ZstdNet)}: Failed to get executing assembly location");
					continue;
				}

				// Nuget package
				if(Path.GetFileName(path).StartsWith("net", StringComparison.Ordinal) && Path.GetFileName(Path.GetDirectoryName(path)) == "lib" && File.Exists(Path.Combine(path, "../../zstdnet.nuspec")))
					path = Path.Combine(path, "../../build");

				var platform = Environment.Is64BitProcess ? "x64" : "x86";
			    var pathToApply = Path.Combine(path, platform);

                if (!SetDllDirectory(pathToApply))
					Trace.TraceWarning($"{nameof(ZstdNet)}: Failed to set DLL directory to '{pathToApply}'");

			    processedLocations.Add(location);
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool SetDllDirectory(string path);

		private const string DllName = "libzstd";

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZDICT_trainFromBuffer(byte[] dictBuffer, size_t dictBufferCapacity, byte[] samplesBuffer, size_t[] samplesSizes, uint nbSamples);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint ZDICT_isError(size_t code);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ZDICT_getErrorName(size_t code);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ZStdCompressionHandle ZSTD_createCCtx();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_freeCCtx(IntPtr handle);
		
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ZStdDecompressionHandle ZSTD_createDCtx();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_freeDCtx(IntPtr dctx);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_compressCCtx(ZStdCompressionHandle ctx, IntPtr dst, size_t dstCapacity, IntPtr src, size_t srcSize, int compressionLevel);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_decompressDCtx(ZStdDecompressionHandle ctx, IntPtr dst, size_t dstCapacity, IntPtr src, size_t srcSize);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ZSTD_createCDict(byte[] dict, size_t dictSize, int compressionLevel);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_freeCDict(IntPtr cdict);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_compress_usingCDict(ZStdCompressionHandle cctx, IntPtr dst, size_t dstCapacity, IntPtr src, size_t srcSize, IntPtr cdict);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ZSTD_createDDict(byte[] dict, size_t dictSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_freeDDict(IntPtr ddict);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_decompress_usingDDict(ZStdDecompressionHandle dctx, IntPtr dst, size_t dstCapacity, IntPtr src, size_t srcSize, IntPtr ddict);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ulong ZSTD_getDecompressedSize(IntPtr src, size_t srcSize);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int ZSTD_maxCLevel();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern size_t ZSTD_compressBound(size_t srcSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint ZSTD_isError(size_t code);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ZSTD_getErrorName(size_t code);
	}

	internal class ZStdCompressionHandle : SafeHandleZeroOrMinusOneIsInvalid {
	    internal ZStdCompressionHandle() : base(true)
	    {
	    }

        internal ZStdCompressionHandle(IntPtr ptr, bool owned)
            : base(owned) => SetHandle(ptr);

        protected override bool ReleaseHandle() 
			=> (!IsInvalid && ExternMethods.ZSTD_freeCCtx(handle) == UIntPtr.Zero);
	}

	internal class ZStdDecompressionHandle : SafeHandleZeroOrMinusOneIsInvalid {

		internal ZStdDecompressionHandle()
			: base(true) 
		{
		}

        internal ZStdDecompressionHandle(IntPtr ptr, bool owned)
            : base(owned) => SetHandle(ptr);

        protected override bool ReleaseHandle() 
			=> (!IsInvalid && ExternMethods.ZSTD_freeDCtx(handle) == UIntPtr.Zero);
	}
}
