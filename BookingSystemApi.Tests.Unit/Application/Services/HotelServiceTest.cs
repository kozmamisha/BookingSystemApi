using AutoMapper;
using BookingSystemApi.Application.Dto;
using BookingSystemApi.Application.Exceptions;
using BookingSystemApi.Application.Interfaces;
using BookingSystemApi.Application.Services;
using BookingSystemApi.Core.Entities;
using BookingSystemApi.Persistence.Interfaces;
using Moq;
using FluentAssertions;

namespace BookingSystemApi.Tests.Unit.Application.Services;

public class HotelServiceTest
{
    private Mock<IHotelRepository> _hotelRepositoryMock;
    private Mock<IMapper> _mapperMock;
    private IHotelService _hotelService;

    [SetUp]
    public void SetUp()
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
    public async Task GetByIdAsync_ShouldReturnsHotelDto_WhenHotelExists()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var hotel = new HotelEntity { Id = hotelId };
        var hotelDto = new HotelDto { Id = hotelId };

        _hotelRepositoryMock
            .Setup(r => r.GetHotelById(hotelId, cancellationToken))
            .ReturnsAsync(hotel);

        _mapperMock
            .Setup(m => m.Map<HotelDto>(hotel))
            .Returns(hotelDto);

        // Act
        var result = await _hotelService.GetByIdAsync(hotelId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(hotelDto);

        _hotelRepositoryMock.Verify(
            r => r.GetHotelById(hotelId, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ShouldThrowsEntityNotFoundException_WhenHotelDoesNotExist()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _hotelRepositoryMock
            .Setup(r => r.GetHotelById(hotelId, cancellationToken))
            .ReturnsAsync((HotelEntity?)null);

        // Act
        var act = async () => await _hotelService.GetByIdAsync(hotelId, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<EntityNotFoundException>()
            .WithMessage("Hotel not found");
    }

    [Test]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenAnyParameterIsEmpty()
    {
        //Arrange
        var name = "";
        var address = "";
        var description = "";
        var cancellationToken = CancellationToken.None;
        
        // Act
        var act = async () => await _hotelService.CreateAsync(name, address, description, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Name, address, or description cannot be empty.");
        
        _hotelRepositoryMock.Verify(
            r => r.AddHotel(It.IsAny<HotelEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Test]
    public async Task CreateAsync_ShouldCreateHotel_WhenDataIsValid()
    {
        //Arrange
        var name = "test";
        var address = "test";
        var description = "test";
        var cancellationToken = CancellationToken.None;
        
        // Act
        await _hotelService.CreateAsync(name, address, description, cancellationToken);

        // Assert
        _hotelRepositoryMock.Verify(
            r => r.AddHotel(
                It.Is<HotelEntity>(h =>
                    h.Name == name &&
                    h.Address == address &&
                    h.Description == description),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Test]
    public async Task UpdateAsync_ShouldThrowArgumentException_WhenAnyParameterIsInvalid()
    {
        // Act
        var act = async () => await _hotelService.UpdateAsync(
            Guid.NewGuid(),
            "",
            "address",
            "description",
            CancellationToken.None);
 
        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Name, address, or description cannot be empty.");
        
        _hotelRepositoryMock.Verify(
            r => r.UpdateHotel(It.IsAny<HotelEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Test]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenHotelDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        _hotelRepositoryMock
            .Setup(r => r.GetHotelById(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HotelEntity?)null);

        // Act
        var act = async () => await _hotelService.UpdateAsync(
            id,
            "Hotel",
            "Address",
            "Description",
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<EntityNotFoundException>()
            .WithMessage("Hotel not found");

        _hotelRepositoryMock.Verify(
            r => r.UpdateHotel(It.IsAny<HotelEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateHotel_WhenDataIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hotel = new HotelEntity
        {
            Id = id,
            Name = "Old name",
            Address = "Old address",
            Description = "Old description"
        };

        _hotelRepositoryMock
            .Setup(r => r.GetHotelById(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        // Act
        await _hotelService.UpdateAsync(
            id,
            "New name",
            "New address",
            "New description",
            CancellationToken.None);

        // Assert
        _hotelRepositoryMock.Verify(
            r => r.UpdateHotel(
                It.Is<HotelEntity>(h =>
                    h.Id == id &&
                    h.Name == "New name" &&
                    h.Address == "New address" &&
                    h.Description == "New description"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Test]
    public async Task DeleteAsync_ShouldDeleteHotel_WhenHotelExists()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var hotel = new HotelEntity
        {
            Id = hotelId, 
            Name = "Test Hotel", 
            Address = "Test address", 
            Description = "Test description"
        };

        _hotelRepositoryMock
            .Setup(r => r.GetHotelById(hotelId, cancellationToken))
            .ReturnsAsync(hotel);

        // Act
        await _hotelService.DeleteAsync(hotelId, cancellationToken);

        // Assert
        _hotelRepositoryMock.Verify(
            r => r.DeleteHotel(hotel, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ShouldThrowsEntityNotFoundException_WhenHotelDoesNotExist()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _hotelRepositoryMock
            .Setup(r => r.GetHotelById(hotelId, cancellationToken))
            .ReturnsAsync((HotelEntity?)null);

        // Act
        var act = async () => await _hotelService.DeleteAsync(hotelId, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<EntityNotFoundException>()
            .WithMessage("Hotel not found");
        
        _hotelRepositoryMock.Verify(
            r => r.DeleteHotel(It.IsAny<HotelEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}