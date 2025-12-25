using AutoMapper;
using BookingSystemApi.Application.Interfaces;
using BookingSystemApi.Application.Services;
using BookingSystemApi.Persistence.Interfaces;
using Moq;

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
}