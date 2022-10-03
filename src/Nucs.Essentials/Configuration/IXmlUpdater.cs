
namespace Nucs.Configuration {
    /// <summary>
    ///     A class with a simple delegate that manipulates <see cref="XmlConfig"/>
    /// </summary>
    public interface IXmlUpdater {
        public XmlConfig Update(XmlConfig cfg);
    }
}