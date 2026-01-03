using AutoMapper;
using BookingSystemApi.Application.Dto;
using BookingSystemApi.Application.Interfaces;
using BookingSystemApi.Application.Services;
using BookingSystemApi.Core.Entities;
using BookingSystemApi.Persistence.Interfaces;
using Moq;
using Xunit;
using FluentAssertions;

namespace BookingSystemApi.Tests.Unit.Application.Services;

public class HotelServiceTest
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IHotelService _hotelService;

    public HotelServiceTest()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _mapperMock = new Mock<IMapper>();
        _hotelService = new HotelService(_hotelRepositoryMock.Object, _mapperMock.Object);
    }
    
    [Test]
    public async Task GetAllAsync_ReturnsMappedHotels()
    {
        // Arrange
        var hotels = new List<HotelEntity>
        {
            new() { Name = "Hotel 1", Address = "Addr 1", Description = "Desc 1" },
            new() { Name = "Hotel 2", Address = "Addr 2", Description = "Desc 2" }
        };

        _hotelRepositoryMock.Setup(r => r.GetAllHotels(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotels);

        _mapperMock.Setup(m => m.Map<List<HotelDto>>(hotels))
            .Returns(new List<HotelDto>
            {
                new() { Name = "Hotel 1", Address = "Addr 1", Description = "Desc 1" },
                new() { Name = "Hotel 2", Address = "Addr 2", Description = "Desc 2" }
            });

        // Act
        var result = await _hotelService.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Hotel 1");
    }
    
    [Test]
    public void Runner_Works()
    {
        true.Should().BeTrue();
    }

}