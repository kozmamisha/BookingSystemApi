
using AutoMapper;
using BookingSystemApi.Application.Dto;
using BookingSystemApi.Application.Exceptions;
using BookingSystemApi.Application.Interfaces;
using BookingSystemApi.Application.Services;
using BookingSystemApi.Core.Entities;
using BookingSystemApi.Persistence.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace BookingSystemApi.Tests.Unit.Application.Services;

public class BookingServiceTests
{
    private Mock<IBookingRepository> _bookingRepositoryMock;
    private Mock<IRoomRepository> _roomRepositoryMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IMapper> _mapperMock;
    private Mock<UserManager<UserEntity>> _userManagerMock;

    private IBookingService _bookingService;

    [SetUp]
    public void SetUp()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _mapperMock = new Mock<IMapper>();

        _userManagerMock = new Mock<UserManager<UserEntity>>(
            Mock.Of<IUserStore<UserEntity>>(),
            null, null, null, null, null, null, null, null);

        _bookingService = new BookingService(
            _bookingRepositoryMock.Object,
            _roomRepositoryMock.Object,
            _httpContextAccessorMock.Object,
            _mapperMock.Object,
            _userManagerMock.Object);
    }
    
    [Test]
    public async Task GetAllAsync_ReturnMappedBookingDtos()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var bookings = new List<BookingEntity>
        {
            new BookingEntity { Id = Guid.NewGuid() },
            new BookingEntity { Id = Guid.NewGuid() }
        };

        var bookingDtos = new List<BookingDto>
        {
            new BookingDto { Id = bookings[0].Id },
            new BookingDto { Id = bookings[1].Id }
        };

        _bookingRepositoryMock
            .Setup(r => r.GetAllBookings(cancellationToken))
            .ReturnsAsync(bookings);

        _mapperMock
            .Setup(m => m.Map<List<BookingDto>>(bookings))
            .Returns(bookingDtos);

        // Act
        var result = await _bookingService.GetAllAsync(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(bookingDtos);

        _bookingRepositoryMock.Verify(
            r => r.GetAllBookings(cancellationToken),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<List<BookingDto>>(bookings),
            Times.Once);
    }
    
    [Test]
    public async Task GetAllByUserIdAsync_ShouldReturnMappedBookings_ForGivenUserId()
    {
        // Arrange
        var userId = "user-123";
        var cancellationToken = CancellationToken.None;

        var bookings = new List<BookingEntity>
        {
            new BookingEntity { Id = Guid.NewGuid(), UserId = userId },
            new BookingEntity { Id = Guid.NewGuid(), UserId = userId }
        };

        var bookingDtos = new List<BookingDto>
        {
            new BookingDto { Id = bookings[0].Id },
            new BookingDto { Id = bookings[1].Id }
        };

        _bookingRepositoryMock
            .Setup(r => r.GetAllBookingsByUserId(userId, cancellationToken))
            .ReturnsAsync(bookings);

        _mapperMock
            .Setup(m => m.Map<List<BookingDto>>(bookings))
            .Returns(bookingDtos);

        // Act
        var result = await _bookingService.GetAllByUserIdAsync(userId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(bookingDtos);

        _bookingRepositoryMock.Verify(
            r => r.GetAllBookingsByUserId(userId, cancellationToken),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<List<BookingDto>>(bookings),
            Times.Once);
    }
    
    [Test]
    public async Task GetAllByUserIdAsync_ShouldReturnEmptyList_WhenNoBookings()
    {
        // Arrange
        _bookingRepositoryMock
            .Setup(r => r.GetAllBookingsByUserId(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>());

        _mapperMock
            .Setup(m => m.Map<List<BookingDto>>(It.IsAny<List<BookingEntity>>()))
            .Returns(new List<BookingDto>());

        // Act
        var result = await _bookingService.GetAllByUserIdAsync("user-123", CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
    
    [Test]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenDatesAreInvalid()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime;
        var roomId = Guid.NewGuid();
        var userId = "user-1";

        // Act
        var act = async () =>
            await _bookingService.CreateAsync(
                startTime, endTime, roomId, userId, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Invalid booking dates");
    }
    
    [Test]
    public async Task CreateAsync_ShouldThrowEntityNotFoundException_WhenRoomNotFound()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);
        var roomId = Guid.NewGuid();
        var userId = "user-1";

        _roomRepositoryMock
            .Setup(r => r.GetRoomById(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomEntity?)null);

        // Act
        var act = async () =>
            await _bookingService.CreateAsync(
                startTime, endTime, roomId, userId, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<EntityNotFoundException>()
            .WithMessage("Room not found");
    }
    
    [Test]
    public async Task CreateAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);
        var roomId = Guid.NewGuid();
        var userId = "user-1";

        _roomRepositoryMock
            .Setup(r => r.GetRoomById(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoomEntity { Id = roomId });

        _userManagerMock
            .Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync((UserEntity?)null);

        // Act
        var act = async () =>
            await _bookingService.CreateAsync(
                startTime, endTime, roomId, userId, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<EntityNotFoundException>()
            .WithMessage("User not found");
    }
    
    [Test]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenRoomIsNotAvailable()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);
        var roomId = Guid.NewGuid();
        var userId = "user-1";

        _roomRepositoryMock
            .Setup(r => r.GetRoomById(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoomEntity { Id = roomId });

        _userManagerMock
            .Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync(new UserEntity { Id = userId });

        _bookingRepositoryMock
            .Setup(r => r.GetOverlappingBookingsAsync(
                roomId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>
            {
                new BookingEntity()
            });

        // Act
        var act = async () =>
            await _bookingService.CreateAsync(
                startTime, endTime, roomId, userId, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Room is not available for the selected dates");
    }

    [Test]
    public async Task CreateAsync_ShouldAddBooking_WhenDataIsValid()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);
        var roomId = Guid.NewGuid();
        var userId = "user-1";

        _roomRepositoryMock
            .Setup(r => r.GetRoomById(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoomEntity { Id = roomId });

        _userManagerMock
            .Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync(new UserEntity { Id = userId });

        _bookingRepositoryMock
            .Setup(r => r.GetOverlappingBookingsAsync(
                roomId, startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>());

        // Act
        await _bookingService.CreateAsync(
            startTime, endTime, roomId, userId, CancellationToken.None);

        // Assert
        _bookingRepositoryMock.Verify(
            r => r.AddBooking(
                It.Is<BookingEntity>(b =>
                    b.RoomId == roomId &&
                    b.UserId == userId &&
                    b.StartTime == startTime &&
                    b.EndTime == endTime),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }




}