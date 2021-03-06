using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Moq;
using FluentAssertions;
using SprayChronicle.EventSourcing;
using SprayChronicle.Example.Domain;
using SprayChronicle.Example.Contracts.Events;

namespace SprayChronicle.Test.EventSourcing
{
    public class EventSourcedRepositoryTest
    {
        public Mock<IEventStore> Persistence = new Mock<IEventStore>();

        [Fact]
        public void ItAppendsMessages()
        {
            new EventSourcedRepository<Basket>(Persistence.Object).Save(
                Basket.PickUp(new BasketId("foo"))
            );
            
            Persistence.Verify(p => p.Append<Basket>(
                It.Is<string>(i => i == "foo"),
                It.Is<IEnumerable<DomainMessage>>(i => IsEqual(i.Select(dm => dm.Payload), new object[] { new BasketPickedUp("foo") })
            )));
        }

        [Fact]
        public void ItLoadsMessages()
        {
            Persistence.Setup(p => p
                .Load<Basket>(
                    It.Is<string>(i => i == "foo")))
                .Returns(new DomainMessage[] { new DomainMessage(0, new DateTime(), new BasketPickedUp("foo")) });
                
            new EventSourcedRepository<Basket>(Persistence.Object).Load("foo").Should().BeAssignableTo<PickedUpBasket>();
        }

        bool IsEqual(object first, object second)
        {
            try {
                first.ShouldBeEquivalentTo(second);
            } catch (Exception) {
                return false;
            }
            return true;
        }
    }
}