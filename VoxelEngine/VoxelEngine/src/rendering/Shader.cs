using System.Numerics;
using Silk.NET.OpenGL;

namespace VoxelEngine.src.rendering;

public class Shader
{
    private GL gl;
    private uint handle;
    private readonly Dictionary<string, int> uniformLocationCache = new();

    public Shader(GL gl, uint handle)
    {
        this.gl = gl;
        this.handle = handle;
    }
    public void Use()
    {
        gl.UseProgram(handle);
    }
    public unsafe void SetUniform(string name, Matrix4x4 matrix4x4)
    {
        int location = GetUniformLocation(name);
        gl.UniformMatrix4(location, 1, false, (float*)&matrix4x4);
    }
    private int GetUniformLocation(string name)
    {
        if (uniformLocationCache.TryGetValue(name, out int location))
            return location;

        location = gl.GetUniformLocation(handle, name);
        uniformLocationCache[name] = location;
        return location;
    }

    public static Shader CreateShader(GL gl, string vertexPath, string fragmentPath)
    {
        string vertSrc = LoadShaderSource(vertexPath);
        string fragSrc = LoadShaderSource(fragmentPath);

        uint vertShader = CompileShader(gl, ShaderType.VertexShader, vertSrc);
        uint fragShader = CompileShader(gl, ShaderType.FragmentShader, fragSrc);

        uint handle = gl.CreateProgram();
        gl.AttachShader(handle, vertShader);
        gl.AttachShader(handle, fragShader);
        gl.LinkProgram(handle);

        string programLog = gl.GetProgramInfoLog(handle);
        if (!string.IsNullOrEmpty(programLog))
            throw new Exception($"Error linking shader program: {programLog}");

        gl.DetachShader(handle, vertShader);
        gl.DetachShader(handle, fragShader);
        gl.DeleteShader(vertShader);
        gl.DeleteShader(fragShader);

        return new Shader(gl, handle);
    }
    private static string LoadShaderSource(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shader file missing at: {path}");

        string source = File.ReadAllText(path);
        return source;
    }
    private static uint CompileShader(GL gl, ShaderType type, string source)
    {
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);

        string shaderLog = gl.GetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(shaderLog))
            throw new Exception($"Error compiling {type}: {shaderLog}");

        return shader;
    }
}
