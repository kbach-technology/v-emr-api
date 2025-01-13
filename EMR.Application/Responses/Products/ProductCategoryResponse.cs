namespace EMR.Application.Responses.Products;

public record ProductCategoryResponse(
    string Id,
    string Name,
    int CommissionRate,
    string Icon,
    bool HasVariant
);

public record ProductCategoryDetailResponse(
    string Id,
    string Name,
    int CommissionRate,
    string Icon,
    bool HasVariant,
    bool IsActive,
    bool IsPublished,
    string CreatedBy,
    DateTime CreatedOn
);