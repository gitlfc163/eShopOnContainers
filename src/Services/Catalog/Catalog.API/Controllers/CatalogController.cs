﻿namespace Microsoft.eShopOnContainers.Services.Catalog.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class CatalogController : ControllerBase
{
    private readonly CatalogContext _catalogContext;
    private readonly CatalogSettings _settings;
    private readonly ICatalogIntegrationEventService _catalogIntegrationEventService;

    public CatalogController(CatalogContext context, IOptionsSnapshot<CatalogSettings> settings, ICatalogIntegrationEventService catalogIntegrationEventService)
    {
        _catalogContext = context ?? throw new ArgumentNullException(nameof(context));
        _catalogIntegrationEventService = catalogIntegrationEventService ?? throw new ArgumentNullException(nameof(catalogIntegrationEventService));
        _settings = settings.Value;

        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    // GET api/v1/[controller]/items[?pageSize=3&pageIndex=10]
    /// <summary>
    /// 查询商品并分页
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("items")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(IEnumerable<CatalogItem>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ItemsAsync([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, string ids = null)
    {
        if (!string.IsNullOrEmpty(ids))
        {
            var items = await GetItemsByIdsAsync(ids);

            if (!items.Any())
            {
                return BadRequest("ids value invalid. Must be comma-separated list of numbers");
            }

            return Ok(items);
        }

        var totalItems = await _catalogContext.CatalogItems
            .LongCountAsync();

        var itemsOnPage = await _catalogContext.CatalogItems
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        /* The "awesome" fix for testing Devspaces */

        /*
        foreach (var pr in itemsOnPage) {
            pr.Name = "Awesome " + pr.Name;
        }

        */

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

        return Ok(model);
    }

    /// <summary>
    /// 查询多个商品详情
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    private async Task<List<CatalogItem>> GetItemsByIdsAsync(string ids)
    {
        var numIds = ids.Split(',').Select(id => (Ok: int.TryParse(id, out int x), Value: x));

        if (!numIds.All(nid => nid.Ok))
        {
            return new List<CatalogItem>();
        }

        var idsToSelect = numIds
            .Select(id => id.Value);

        var items = await _catalogContext.CatalogItems.Where(ci => idsToSelect.Contains(ci.Id)).ToListAsync();

        items = ChangeUriPlaceholder(items);

        return items;
    }
    /// <summary>
    /// 查询商品详情
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("items/{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogItem), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogItem>> ItemByIdAsync(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var item = await _catalogContext.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == id);

        var baseUri = _settings.PicBaseUrl;
        var azureStorageEnabled = _settings.AzureStorageEnabled;

        item.FillProductUrl(baseUri, azureStorageEnabled: azureStorageEnabled);

        if (item != null)
        {
            return item;
        }

        return NotFound();
    }

    // GET api/v1/[controller]/items/withname/samplename[?pageSize=3&pageIndex=10]
    /// <summary>
    /// 根据名字模糊查询
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("items/withname/{name:minlength(1)}")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsWithNameAsync(string name, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var totalItems = await _catalogContext.CatalogItems
            .Where(c => c.Name.StartsWith(name))
            .LongCountAsync();

        var itemsOnPage = await _catalogContext.CatalogItems
            .Where(c => c.Name.StartsWith(name))
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    // GET api/v1/[controller]/items/type/1/brand[?pageSize=3&pageIndex=10]
    /// <summary>
    /// 根据类型+商家查询
    /// </summary>
    /// <param name="catalogTypeId">类型id</param>
    /// <param name="catalogBrandId">商家id</param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("items/type/{catalogTypeId}/brand/{catalogBrandId:int?}")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsByTypeIdAndBrandIdAsync(int catalogTypeId, int? catalogBrandId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var root = (IQueryable<CatalogItem>)_catalogContext.CatalogItems;

        root = root.Where(ci => ci.CatalogTypeId == catalogTypeId);

        if (catalogBrandId.HasValue)
        {
            root = root.Where(ci => ci.CatalogBrandId == catalogBrandId);
        }

        var totalItems = await root
            .LongCountAsync();

        var itemsOnPage = await root
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    // GET api/v1/[controller]/items/type/all/brand[?pageSize=3&pageIndex=10]
    /// <summary>
    /// 根据商家查询
    /// </summary>
    /// <param name="catalogBrandId"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("items/type/all/brand/{catalogBrandId:int?}")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsByBrandIdAsync(int? catalogBrandId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var root = (IQueryable<CatalogItem>)_catalogContext.CatalogItems;

        if (catalogBrandId.HasValue)
        {
            root = root.Where(ci => ci.CatalogBrandId == catalogBrandId);
        }

        var totalItems = await root
            .LongCountAsync();

        var itemsOnPage = await root
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    // GET api/v1/[controller]/CatalogTypes
    /// <summary>
    /// 所有商品类别
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("catalogtypes")]
    [ProducesResponseType(typeof(List<CatalogType>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<CatalogType>>> CatalogTypesAsync()
    {
        return await _catalogContext.CatalogTypes.ToListAsync();
    }

    // GET api/v1/[controller]/CatalogBrands
    /// <summary>
    ///  所有商家
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("catalogbrands")]
    [ProducesResponseType(typeof(List<CatalogBrand>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<CatalogBrand>>> CatalogBrandsAsync()
    {
        return await _catalogContext.CatalogBrands.ToListAsync();
    }

    //PUT api/v1/[controller]/items
    /// <summary>
    /// 更新商品
    /// 发布事件
    /// </summary>
    /// <param name="productToUpdate"></param>
    /// <returns></returns>
    [Route("items")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<ActionResult> UpdateProductAsync([FromBody] CatalogItem productToUpdate)
    {
        var catalogItem = await _catalogContext.CatalogItems.SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

        if (catalogItem == null)
        {
            return NotFound(new { Message = $"Item with id {productToUpdate.Id} not found." });
        }

        var oldPrice = catalogItem.Price;
        //价格是否发生变化
        var raiseProductPriceChangedEvent = oldPrice != productToUpdate.Price;

        // Update current product
        catalogItem = productToUpdate;
        _catalogContext.CatalogItems.Update(catalogItem);

        if (raiseProductPriceChangedEvent) // 如果价格发生变化，保存产品数据并通过事件总线发布集成事件
        {
            //声明事件源 创建要通过事件总线发布的集成事件
            var priceChangedEvent = new ProductPriceChangedIntegrationEvent(catalogItem.Id, productToUpdate.Price, oldPrice);

            // 由于本地事务，在原始目录数据库操作和 IntegrationEventLog 之间实现原子性
            await _catalogIntegrationEventService.SaveEventAndCatalogContextChangesAsync(priceChangedEvent);

            // 通过事件总线发布并将保存的事件标记为已发布
            await _catalogIntegrationEventService.PublishThroughEventBusAsync(priceChangedEvent);
        }
        else // 只需保存更新的产品，因为产品的价格没有改变.
        {
            await _catalogContext.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(ItemByIdAsync), new { id = productToUpdate.Id }, null);
    }

    //POST api/v1/[controller]/items
    /// <summary>
    /// 添加商品
    /// </summary>
    /// <param name="product"></param>
    /// <returns></returns>
    [Route("items")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<ActionResult> CreateProductAsync([FromBody] CatalogItem product)
    {
        var item = new CatalogItem
        {
            CatalogBrandId = product.CatalogBrandId,
            CatalogTypeId = product.CatalogTypeId,
            Description = product.Description,
            Name = product.Name,
            PictureFileName = product.PictureFileName,
            Price = product.Price
        };

        _catalogContext.CatalogItems.Add(item);

        await _catalogContext.SaveChangesAsync();

        return CreatedAtAction(nameof(ItemByIdAsync), new { id = item.Id }, null);
    }

    //DELETE api/v1/[controller]/id
    /// <summary>
    /// 删除商品
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Route("{id}")]
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteProductAsync(int id)
    {
        var product = _catalogContext.CatalogItems.SingleOrDefault(x => x.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        _catalogContext.CatalogItems.Remove(product);

        await _catalogContext.SaveChangesAsync();

        return NoContent();
    }
    /// <summary>
    /// 修改图片地址
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    private List<CatalogItem> ChangeUriPlaceholder(List<CatalogItem> items)
    {
        var baseUri = _settings.PicBaseUrl;
        var azureStorageEnabled = _settings.AzureStorageEnabled;

        foreach (var item in items)
        {
            item.FillProductUrl(baseUri, azureStorageEnabled: azureStorageEnabled);
        }

        return items;
    }
}
