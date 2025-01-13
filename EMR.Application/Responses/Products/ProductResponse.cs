using System.Collections.Generic;

namespace EMR.Application.Responses.Products;

public record ProductResponse(
    string Id,
    string Name,
    string ProductCode,
    string Brand,
    string SKU,
    string Description,
    decimal Price,
    bool IsBundled,
    bool IsActive,
    bool IsPublished,
    ProductCategoryResponse Category
);

public record ProductMinimalResponse(
    string Id,
    string Name,
    string ImageUrl,
    decimal Price
);

public record ProductDetailResponse(
    string Id,
    string Name,
    string ProductCode,
    string Brand,
    string SKU,
    string Description,
    decimal Price,
    bool IsBundled,
    bool IsActive,
    bool IsPublished,
    ProductCategoryResponse Category,
    List<ProductMediaResponse> Medias,
    List<ProductVariantResponse> ProductVariants,
    List<ProductBundleResponse> Bundles
);

public record ProductMediaResponse(
    string Id,
    string MediaUrl,
    string MediaType,
    string Size,
    string Extension
);

public record ProductVariantResponse(
    string Id,
    string Name,
    decimal Price,
    bool IsActive,
    List<VariantResponse> Variants
);

public record VariantResponse(
    string Id,
    string Value,
    decimal Price
);

public record ProductBundleResponse(
    string Id,
    string Name
);