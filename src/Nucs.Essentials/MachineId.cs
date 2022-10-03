using System;
using System.IO;

namespace Nucs {
    /// <summary>
    ///     Provides unique <see cref="Guid"/> based on the machine and user.
    /// </summary>
    public static class MachineId {
        private static Guid? _machine;
        private static Guid? _user;

        /// <summary>
        ///     Id unique to the machine, shared between users.
        /// </summary>
        public static Guid Machine => _machine ?? (_machine = _getId(false)).Value;

        /// <summary>
        ///     Id unique to the machine and to currently logged in user.
        /// </summary>
        public static Guid User => _user ?? (_user = _getId(true)).Value;

        private static Guid _getId(bool isUser) {
            string path = isUser ? Environment.ExpandEnvironmentVariables("%APPDATA%/.user.id") : Environment.ExpandEnvironmentVariables("%APPDATA%/.machine.id");
            if (!File.Exists(path)) {
                Guid ret;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, (ret = Guid.NewGuid()).ToByteArray());

                return ret;
            }

            return new Guid(File.ReadAllBytes(path));
        }
    }
}