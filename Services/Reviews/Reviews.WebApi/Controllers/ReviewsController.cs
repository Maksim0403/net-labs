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

    /// <summary>Створення нового відгуку.</summary>
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

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Отримання відгуку за його ідентифікатором.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetReviewByIdQuery(id), ct);
        if (result is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Не знайдено",
                Detail = $"Відгук з id '{id}' не знайдено."
            });

        return Ok(result);
    }

    /// <summary>Отримання пагінованого списку відгуків з фільтрацією.</summary>
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

    /// <summary>Оновлення відгуку (з перевіркою версії для оптимістичної конкурентності).</summary>
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
        var command = new UpdateReviewCommand(id, request.Rating, request.Text, request.Criteria, request.Version);
        await _sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>Видалення відгуку (з перевіркою версії для оптимістичної конкурентності).</summary>
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

    /// <summary>Додавання коментаря до відгуку.</summary>
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
        var command = new AddCommentCommand(id, request.Author, request.Text, request.Version);
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }
}

