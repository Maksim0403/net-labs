using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Commands;
using Reviews.Application.Common;
using Reviews.Application.DTOs;
using Reviews.Application.Queries;
using Reviews.WebApi.Models;

namespace Reviews.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ReviewsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public ReviewsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    /// <summary>Створення нового відгуку (HTTP 201 Created з Location-заголовком).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        var command = _mapper.Map<CreateReviewCommand>(request);
        var result = await _sender.Send(command, ct);

        // Передаємо рядковий ObjectId у заголовку Location
        var locationUri = Url.Action(nameof(GetById), new { id = result.Id }) ?? $"/api/reviews/{result.Id}";
        Response.Headers.Location = locationUri;

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Отримання відгуку за його ідентифікатором (HTTP 200 OK або 404 Not Found).</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetReviewByIdQuery(id), ct);
        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не знайдено",
                Detail = $"Відгук з id '{id}' не знайдено."
            });
        }

        return Ok(result);
    }

    /// <summary>Отримання пагінованого списку відгуків з фільтрацією (HTTP 200 OK).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList(
        [FromQuery] long? courierId,
        [FromQuery] long? customerId,
        [FromQuery] long? shipmentId,
        [FromQuery] int? minRating,
        [FromQuery] string? searchText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetReviewsQuery(courierId, customerId, shipmentId, minRating, searchText, page, pageSize);
        var result = await _sender.Send(query, ct);
        return Ok(result);
    }

    /// <summary>Оновлення відгуку за ідентифікатором (HTTP 204 No Content або 400/404/409).</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] string id,
        [FromBody] UpdateReviewRequest request,
        CancellationToken ct)
    {
        var command = _mapper.Map<UpdateReviewCommand>(request) with { Id = id };
        await _sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>Видалення відгуку за ідентифікатором (HTTP 204 No Content або 400/404/409).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] string id,
        [FromQuery] int version,
        CancellationToken ct)
    {
        var command = new DeleteReviewCommand(id, version);
        await _sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>Додавання коментаря до відгуку (HTTP 200 OK або 400/404/409).</summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddComment(
        [FromRoute] string id,
        [FromBody] AddCommentRequest request,
        CancellationToken ct)
    {
        var command = _mapper.Map<AddCommentCommand>(request) with { ReviewId = id };
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }
}

