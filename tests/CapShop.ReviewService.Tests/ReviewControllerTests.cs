using System.Security.Claims;
using CapShop.ReviewService.Application.Interfaces;
using CapShop.ReviewService.Controllers;
using CapShop.ReviewService.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.ReviewService.Tests;

public class ReviewControllerTests
{
    [Test]
    public async Task GetProductSummary_WhenUserClaimPresent_ForwardsUserId()
    {
        var summary = new ProductReviewSummaryDto
        {
            ProductId = 9,
            AverageRating = 4.5m,
            ReviewCount = 2,
            CanCurrentUserReview = true
        };

        var reviewService = new Mock<IReviewAppService>();
        reviewService
            .Setup(service => service.GetProductSummaryAsync(9, 18, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var controller = new ReviewController(reviewService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("userId", "18")], "test"))
                }
            }
        };

        var result = await controller.GetProductSummary(9, CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        reviewService.Verify(service => service.GetProductSummaryAsync(9, 18, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateReview_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var reviewService = new Mock<IReviewAppService>();
        reviewService
            .Setup(service => service.CreateReviewAsync(
                9,
                18,
                "Priya",
                It.IsAny<CreateReviewRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewResponseDto
            {
                Id = 7,
                ProductId = 9,
                UserId = 18,
                UserName = "Priya",
                Rating = 5,
                Title = "Great product",
                Comment = "Works well",
                IsVerifiedPurchase = true,
                Status = "Approved"
            });

        var controller = new ReviewController(reviewService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim("userId", "18"),
                            new Claim("name", "Priya")
                        ],
                        "test"))
                }
            }
        };

        var result = await controller.CreateReview(9, new CreateReviewRequestDto
        {
            Rating = 5,
            Title = "Great product",
            Comment = "Works well"
        }, CancellationToken.None);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.ActionName, Is.EqualTo(nameof(ReviewController.GetProductReviews)));
        reviewService.Verify(service => service.CreateReviewAsync(
            9,
            18,
            "Priya",
            It.IsAny<CreateReviewRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
