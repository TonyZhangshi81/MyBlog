using System.Linq;
using System.Threading.Tasks;
using Moq;
using MyBlog.Business.Commands;
using MyBlog.Business.IO;
using MyBlog.Data;
using Xunit;

namespace MyBlog.Business.Test.Commands;

public class AddImageCommandHandlerTest
{
    private readonly EFUnitOfWork unitOfWork;

    private readonly Mock<IImageFileProvider> imageFileProviderMock;

    public AddImageCommandHandlerTest()
    {
        this.unitOfWork = new InMemoryDatabaseFactory().CreateContext();
        this.imageFileProviderMock = new Mock<IImageFileProvider>();
    }

    [Fact]
    public async Task AddImage()
    {
        var sut = new AddImageCommandHandler(this.unitOfWork, this.imageFileProviderMock.Object);
        await sut.HandleAsync(new AddImageCommand("path\\test.png", new byte[0]));

        var images = this.unitOfWork.Images.Where(i => i.Name == "test.png").ToList();

        Assert.Single(images);
        Assert.Equal($"{images[0].Id}.png", images[0].Path);

        this.imageFileProviderMock.Verify(i => i.AddFileAsync($"{images[0].Id}.png", It.IsAny<byte[]>()), Times.Once);
    }
}
