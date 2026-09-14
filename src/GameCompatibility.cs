using System;
using System.IO;
using System.Security.Cryptography;

namespace SailwindFastForward
{
    internal static class GameCompatibility
    {
        // Reinspect save boundaries and serialization before accepting another build.
        internal const string AssemblySha256 = "978A21A680F42C89EBCB3530F9A99EF074960BE6377DAF8E85893134A5E5CE23";

        internal static void RequireSupported(string assemblyPath)
        {
            using (var stream = File.OpenRead(assemblyPath))
                if (!IsSupported(stream))
                    throw new NotSupportedException("This Sailwind assembly has not been verified for fast-forward save handling. Fast-forward is disabled until the game build is reinspected.");
        }

        internal static bool IsSupported(Stream assembly)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(assembly)).Replace("-", "") == AssemblySha256;
        }
    }
}
