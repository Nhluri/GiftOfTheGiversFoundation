using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

namespace GiftOfTheGiversFoundation.UnitTests.Helpers
{
    public static class MockSession
    {
        public static ISession CreateMockSession()
        {
            var sessionMock = new Mock<ISession>();
            var sessionData = new Dictionary<string, byte[]>();

            sessionMock
                .Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => sessionData[key] = value);

            sessionMock
                .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    value = sessionData.ContainsKey(key) ? sessionData[key] : null;
                    return value != null;
                });

            return sessionMock.Object;
        }
    }
}