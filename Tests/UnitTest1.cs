using LibSugar.Future;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void Test1()
    {
        Foo2().WaitSync();
    }

    public async Future Foo()
    {
        await Future.Delay(100);
    }

    public async Future Foo2()
    {
        await Foo();
    }

}
