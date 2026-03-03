using IssueManagement.Infrastructure.IntegrationTests.Infrastructure;

namespace IssueManagement.Infrastructure.IntegrationTests.Storage;

public class MinioBlobStorageServiceTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static MemoryStream CreateTestStream(string content = "Hello, MinIO!")
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
     
    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsSuccessWithObjectName()
    {
        // Arrange
        var objectName = $"test/{Guid.NewGuid()}.txt";
        using var stream = CreateTestStream();

        // Act
        var result = await _blobStorageService.UploadAsync(objectName, stream, "text/plain");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(objectName, result.Value);
    }

    [Fact]
    public async Task UploadAsync_ImageContentType_ReturnsSuccess()
    {
        // Arrange
        var objectName = $"photos/{Guid.NewGuid()}.jpg";
        using var stream = CreateTestStream("fake image data");

        // Act
        var result = await _blobStorageService.UploadAsync(objectName, stream, "image/jpeg");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(objectName, result.Value);
    }

    [Fact]
    public async Task UploadAsync_NestedPath_ReturnsSuccess()
    {
        // Arrange
        var issueId = Guid.NewGuid();
        var objectName = $"{issueId}/photos/{Guid.NewGuid()}.png";
        using var stream = CreateTestStream("nested file");

        // Act
        var result = await _blobStorageService.UploadAsync(objectName, stream, "image/png");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(objectName, result.Value);
    } 

    [Fact]
    public async Task GetPresignedUrlAsync_ExistingObject_ReturnsSuccessWithUrl()
    {
        // Arrange — upload a file first
        var objectName = $"test/{Guid.NewGuid()}.txt";
        using var stream = CreateTestStream();
        await _blobStorageService.UploadAsync(objectName, stream, "text/plain");

        // Act
        var result = await _blobStorageService.GetPresignedUrlAsync(objectName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(objectName, result.Value);
        Assert.StartsWith("http", result.Value);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_NonExistingObject_StillGeneratesUrl()
    {
        // MinIO generates presigned URLs without checking object existence
        // Arrange
        var objectName = $"nonexistent/{Guid.NewGuid()}.txt";

        // Act
        var result = await _blobStorageService.GetPresignedUrlAsync(objectName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(objectName, result.Value);
    }
     

    [Fact]
    public async Task DeleteAsync_ExistingObject_ReturnsSuccess()
    {
        // Arrange — upload a file first
        var objectName = $"test/{Guid.NewGuid()}.txt";
        using var stream = CreateTestStream();
        await _blobStorageService.UploadAsync(objectName, stream, "text/plain");

        // Act
        var result = await _blobStorageService.DeleteAsync(objectName);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingObject_StillReturnsSuccess()
    {
        // MinIO's RemoveObject is idempotent — doesn't throw for missing objects
        // Arrange
        var objectName = $"nonexistent/{Guid.NewGuid()}.txt";

        // Act
        var result = await _blobStorageService.DeleteAsync(objectName);

        // Assert
        Assert.True(result.IsSuccess);
    } 

    [Fact]
    public async Task FullLifecycle_Upload_GetUrl_Delete_AllSucceed()
    {
        // Arrange
        var objectName = $"lifecycle/{Guid.NewGuid()}.txt";
        using var stream = CreateTestStream("lifecycle test content");

        // Act — Upload
        var uploadResult = await _blobStorageService.UploadAsync(objectName, stream, "text/plain");
        Assert.True(uploadResult.IsSuccess);

        // Act — Get presigned URL
        var urlResult = await _blobStorageService.GetPresignedUrlAsync(objectName);
        Assert.True(urlResult.IsSuccess);
        Assert.Contains(objectName, urlResult.Value);

        // Act — Delete
        var deleteResult = await _blobStorageService.DeleteAsync(objectName);
        Assert.True(deleteResult.IsSuccess);
    }

    [Fact]
    public async Task Upload_ThenDelete_ThenGetUrl_UrlStillGenerated()
    {
        // Arrange
        var objectName = $"deleted/{Guid.NewGuid()}.txt";
        using var stream = CreateTestStream("will be deleted");

        // Upload and then delete
        await _blobStorageService.UploadAsync(objectName, stream, "text/plain");
        await _blobStorageService.DeleteAsync(objectName);

        // Act — presigned URL should still be generated (MinIO doesn't check existence)
        var urlResult = await _blobStorageService.GetPresignedUrlAsync(objectName);

        // Assert
        Assert.True(urlResult.IsSuccess);
    } 
}
