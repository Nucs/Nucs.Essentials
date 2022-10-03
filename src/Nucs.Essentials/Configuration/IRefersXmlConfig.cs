namespace Nucs.Configuration {
    /// <summary>
    ///     This object holds a reference to a XmlConfig and is used as ConfigNode.Value.
    /// </summary>
    public interface IRefersXmlConfig {
        public XmlConfig Config { get; set; }
    }
}