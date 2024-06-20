# RenderFormSample


<DIV STYLE="WIDTH:400px">
<img src='https://www.ab4d.com/images/SharpEngine/RenderFormSample.png' height='350' alt='RenderFormSample'/>
</DIV>

To see how to render to the whole Form (Window) at the maximum speed (as many frames per second as possible),
see the **RenderFormSample**.

It can be started by opening the Program.cs file, commenting the start of SamplesForm and uncommenting the start of RenderFormSample:
```
//Application.Run(new SamplesForm());

// Uncomment to run RenderFormSample:
using (var game = new RenderFormSample())
    game.Run();
```

Then restart the application.