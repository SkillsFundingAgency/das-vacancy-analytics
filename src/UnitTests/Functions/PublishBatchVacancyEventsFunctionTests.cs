using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Esfa.Vacancy.Analytics;
using Esfa.Vacancy.Analytics.Events;
using Esfa.VacancyAnalytics.Functions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Functions
{
    public class PublishBatchVacancyEventsFunctionTests
    {
        private const string ApprenticeshipSearchImpressionEventName = nameof(ApprenticeshipSearchImpressionEvent);
        private readonly Mock<IVacancyEventClient> _mockClient;
        private readonly PublishBatchVacancyEvents _sut;

        public PublishBatchVacancyEventsFunctionTests()
        {
            _mockClient = new Mock<IVacancyEventClient>();
            _sut = new PublishBatchVacancyEvents(Mock.Of<ILogger<PublishBatchVacancyEvents>>(), _mockClient.Object);
        }

        [Fact]
        public async Task GivenSingleEventInBatch_ShouldProcess()
        {
            var reqBody = $"{{ \"VacancyRefs\": [\"1000002803\"], \"EventType\": \"{ApprenticeshipSearchImpressionEventName}\" }}";

            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(reqBody))
            };

            var result = await _sut.Run(request, Mock.Of<ILogger>(), null);

            result.GetType().Should().Be(typeof(OkResult));
            _mockClient.Verify(x => x.PublishBatchEventsAsync(It.IsAny<IEnumerable<VacancyEvent>>()), Times.Once);
        }
    }
}