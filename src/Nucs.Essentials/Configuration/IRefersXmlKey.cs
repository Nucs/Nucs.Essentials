namespace Nucs.Configuration {
    /// <summary>
    ///     This object holds a reference to a key of XmlConfig and is used as ConfigNode.Value.
    /// </summary>
    public interface IRefersXmlKey {
        public string Key { get; set; }

        /// <summary>
        ///     Clones and overrides <see cref="Key"/> by subkeying it.
        /// </summary>
        /// <param name="xmlConfig"></param>
        /// <param name="key"></param>
        /// <returns>clone of this with new key</returns>
        public object CloneSub(XmlConfig xmlConfig, string key);
    }
}