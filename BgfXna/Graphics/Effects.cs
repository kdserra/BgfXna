using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class Effect : GraphicsResource
{
    private readonly List<EffectTechnique> _techniques = new();

    public Effect(GraphicsDevice graphicsDevice, byte[] vertexShader, byte[] fragmentShader)
        : base(graphicsDevice)
    {
        BgfxHandle vs = graphicsDevice.Backend.CreateShader(vertexShader, "vertex");
        BgfxHandle fs = graphicsDevice.Backend.CreateShader(fragmentShader, "fragment");
        ProgramHandle = graphicsDevice.Backend.CreateProgram(vs, fs, true);
        Parameters = new EffectParameterCollection();
        CurrentTechnique = new EffectTechnique(this, "Default", new[] { new EffectPass(this, "Pass0") });
        _techniques.Add(CurrentTechnique);
    }

    internal BgfxHandle ProgramHandle { get; }
    public EffectParameterCollection Parameters { get; }
    public EffectTechnique CurrentTechnique { get; set; }
    public IReadOnlyList<EffectTechnique> Techniques => _techniques;

    internal void Apply(GraphicsDevice graphicsDevice)
    {
        graphicsDevice.SetCurrentEffect(this);
        foreach (EffectParameter parameter in Parameters)
        {
            parameter.Apply(graphicsDevice);
        }
    }

    protected override void Dispose(bool disposing) => GraphicsDevice.Backend.Destroy(ProgramHandle);
}

public sealed class EffectTechnique
{
    internal EffectTechnique(Effect effect, string name, IReadOnlyList<EffectPass> passes)
    {
        Effect = effect;
        Name = name;
        Passes = passes;
    }

    public Effect Effect { get; }
    public string Name { get; }
    public IReadOnlyList<EffectPass> Passes { get; }
}

public sealed class EffectPass
{
    internal EffectPass(Effect effect, string name)
    {
        Effect = effect;
        Name = name;
    }

    public Effect Effect { get; }
    public string Name { get; }
    public void Apply() => Effect.Apply(Effect.GraphicsDevice);
}

public sealed class EffectParameterCollection : List<EffectParameter>
{
    public EffectParameter? this[string name] => Find(parameter => parameter.Name == name);
}

public sealed class EffectParameter
{
    private object? _value;

    public EffectParameter(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public object? Value => _value;

    public void SetValue(float value) => _value = value;
    public void SetValue(Vector2 value) => _value = value;
    public void SetValue(Vector3 value) => _value = value;
    public void SetValue(Vector4 value) => _value = value;
    public void SetValue(Matrix value) => _value = value;
    public void SetValue(Texture value) => _value = value;

    internal void Apply(GraphicsDevice graphicsDevice)
    {
        if (_value is Texture texture)
        {
            graphicsDevice.Textures[0] = texture;
        }
    }
}
