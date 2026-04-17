using CapShop.PaymentService.Application.Services;
using CapShop.PaymentService.DTOs;
using CapShop.PaymentService.Infrastructure.Repositories;
using CapShop.PaymentService.Models;
using CapShop.Shared.Exceptions;
using Moq;

namespace CapShop.PaymentService.Tests;

public class PaymentAppServiceTests
{
    [Test]
    public void ProcessPaymentAsync_WhenOrderIdIsInvalid_Throws()
    {
        var repo = new Mock<IPaymentRepository>();
        var sut = new PaymentAppService(repo.Object);

        var action = async () => await sut.ProcessPaymentAsync(11, new ProcessPaymentRequestDto
        {
            OrderId = 0,
            Amount = 250,
            SimulateSuccess = true
        });

        Assert.ThrowsAsync<ValidationException>(async () => await action());
    }

    [Test]
    public void ProcessPaymentAsync_WhenAmountIsInvalid_Throws()
    {
        var repo = new Mock<IPaymentRepository>();
        var sut = new PaymentAppService(repo.Object);

        var action = async () => await sut.ProcessPaymentAsync(11, new ProcessPaymentRequestDto
        {
            OrderId = 1001,
            Amount = 0,
            SimulateSuccess = true
        });

        Assert.ThrowsAsync<ValidationException>(async () => await action());
    }

    [Test]
    public async Task ProcessPaymentAsync_WhenSimulatedSuccess_ReturnsSucceededWithTransactionId()
    {
        var repo = new Mock<IPaymentRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<PaymentRecord>()))
            .ReturnsAsync((PaymentRecord p) =>
            {
                p.Id = 42;
                return p;
            });

        var sut = new PaymentAppService(repo.Object);

        var result = await sut.ProcessPaymentAsync(5, new ProcessPaymentRequestDto
        {
            OrderId = 1001,
            Amount = 999.50m,
            Currency = "INR",
            PaymentMethod = "UPI",
            SimulateSuccess = true
        });

        Assert.That(result.Id, Is.EqualTo(42));
        Assert.That(result.UserId, Is.EqualTo(5));
        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Succeeded));
        Assert.That(result.TransactionId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public async Task ProcessPaymentAsync_WhenSimulatedFailure_ReturnsFailedWithReason()
    {
        var repo = new Mock<IPaymentRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<PaymentRecord>()))
            .ReturnsAsync((PaymentRecord p) => p);

        var sut = new PaymentAppService(repo.Object);

        var result = await sut.ProcessPaymentAsync(5, new ProcessPaymentRequestDto
        {
            OrderId = 1001,
            Amount = 200,
            Currency = "INR",
            PaymentMethod = "Card",
            SimulateSuccess = false
        });

        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Failed));
        Assert.That(result.TransactionId, Is.Null);
        Assert.That(result.FailureReason, Is.EqualTo("Simulated payment failure"));
    }

    [Test]
    public void UpdateStatusAsync_WhenStatusIsInvalid_Throws()
    {
        var repo = new Mock<IPaymentRepository>();
        var sut = new PaymentAppService(repo.Object);

        var action = async () => await sut.UpdateStatusAsync(10, new UpdatePaymentStatusRequestDto
        {
            Status = "Unknown"
        });

        Assert.ThrowsAsync<ValidationException>(async () => await action());
    }
}
