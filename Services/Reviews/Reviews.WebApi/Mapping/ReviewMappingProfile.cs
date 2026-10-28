using AutoMapper;
using MongoDB.Bson;
using Reviews.Application.Commands;
using Reviews.Application.DTOs;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.WebApi.Models;

namespace Reviews.WebApi.Mapping;

/// <summary>Профіль AutoMapper для перетворення запитів, сутностей, Value Objects та DTO.</summary>
public sealed class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        // Мапінг ObjectId <-> string
        CreateMap<ObjectId, string>().ConvertUsing(oid => oid.ToString());
        CreateMap<string, ObjectId>().ConvertUsing(s => ObjectId.Parse(s));

        // Мапінг Value Objects
        CreateMap<Rating, int>().ConvertUsing(r => r.Value);
        CreateMap<int, Rating>().ConvertUsing(v => new Rating(v));

        CreateMap<ReviewCriteria, ReviewCriteriaDto>().ReverseMap();
        CreateMap<ReviewComment, ReviewCommentDto>().ReverseMap();

        // Мапінг доменної сутності Review на ReviewDto (з рядковим ObjectId та вкладеним масивом)
        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.Rating, opt => opt.MapFrom(s => s.Rating.Value))
            .ForMember(d => d.Comments, opt => opt.MapFrom(s => s.Comments));

        // Мапінг API-моделей запитів у CQRS-команди через AutoMapper
        CreateMap<CreateReviewRequest, CreateReviewCommand>();
        CreateMap<UpdateReviewRequest, UpdateReviewCommand>()
            .ForMember(d => d.Id, opt => opt.Ignore());
        CreateMap<AddCommentRequest, AddCommentCommand>()
            .ForMember(d => d.ReviewId, opt => opt.Ignore());
    }
}

