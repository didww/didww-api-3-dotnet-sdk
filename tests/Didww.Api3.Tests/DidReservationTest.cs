using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class DidReservationTest : BaseTest
{
    [Fact]
    public async Task TestListDidReservations()
    {
        StubGet("did_reservations", "did_reservations/index.json");

        var response = await Client.DidReservations().ListAsync();
        var reservations = response.Data;

        reservations.Should().NotBeEmpty();

        var first = reservations[0];
        first.Id.Should().Be("fd38d3ff-80cf-4e67-a605-609a2884a5c4");
        first.ExpireAt.Should().NotBeNull();
        first.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestFindDidReservation()
    {
        StubGet("did_reservations/fd38d3ff-80cf-4e67-a605-609a2884a5c4", "did_reservations/show.json");

        var response = await Client.DidReservations().FindAsync("fd38d3ff-80cf-4e67-a605-609a2884a5c4");
        var reservation = response.Data;

        reservation.Id.Should().Be("fd38d3ff-80cf-4e67-a605-609a2884a5c4");
    }

    [Fact]
    public async Task TestCreateDidReservation()
    {
        StubPost("did_reservations", "did_reservations/create.json");

        var reservation = new DidReservation
        {
            Description = "test reservation",
            AvailableDid = AvailableDid.Build("some-available-did-id")
        };

        var response = await Client.DidReservations().CreateAsync(reservation);
        var created = response.Data;

        created.Id.Should().Be("fd38d3ff-80cf-4e67-a605-609a2884a5c4");
    }

    [Fact]
    public async Task TestDeleteDidReservation()
    {
        var id = "fd38d3ff-80cf-4e67-a605-609a2884a5c4";
        StubDelete("did_reservations/" + id);

        await Client.DidReservations().DeleteAsync(id);
    }
}
