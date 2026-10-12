using NUnit.Framework;
using Usuarios.Api.Infrastructure;

namespace Usuarios.Api.Tests.Infrastructure
{
    [TestFixture]
    public class SecureCompareTests
    {
        [Test]
        public void FixedTimeEquals_ValoresIguales_RetornaTrue()
        {
            Assert.That(SecureCompare.FixedTimeEquals("clave-secreta", "clave-secreta"), Is.True);
        }

        [Test]
        public void FixedTimeEquals_ValoresDistintos_RetornaFalse()
        {
            Assert.That(SecureCompare.FixedTimeEquals("clave-secreta", "otra-clave"), Is.False);
        }

        [Test]
        public void FixedTimeEquals_LongitudesDistintas_RetornaFalse()
        {
            Assert.That(SecureCompare.FixedTimeEquals("corta", "una-clave-mucho-mas-larga"), Is.False);
        }

        [Test]
        public void FixedTimeEquals_ValorNulo_RetornaFalse()
        {
            Assert.That(SecureCompare.FixedTimeEquals(null, "clave"), Is.False);
            Assert.That(SecureCompare.FixedTimeEquals("clave", null), Is.False);
        }

        [Test]
        public void FixedTimeEquals_CadenasVacias_RetornaTrue()
        {
            Assert.That(SecureCompare.FixedTimeEquals(string.Empty, string.Empty), Is.True);
        }
    }
}
