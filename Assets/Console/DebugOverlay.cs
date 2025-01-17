using UnityEngine;

public class DebugOverlay
{
    public int width = 80;
    public int height = 25;

    public static DebugOverlay instance;
    static DebugOverlayResources resources;

    public static int Width { get { return instance.width; } }
    public static int Height { get { return instance.height; } }

    public void Init(int w, int h)
    {
        if(resources == null)
        {
            resources = Resources.Load<DebugOverlayResources>("DebugOverlayResources");
            Debug.Assert(resources != null, "Unable to load DebugOverlayResources");
        }
        if (instance == null)
            instance = this;

        width = w;
        height = h;
    }

    public void Shutdown()
    {
        if (m_QuadInstanceBuffer != null)
            m_QuadInstanceBuffer.Release();
        m_QuadInstanceBuffer = null;
        m_QuadInstanceData = null;

        if (m_LineInstanceBuffer != null)
            m_LineInstanceBuffer.Release();
        m_LineInstanceBuffer = null;
        m_LineInstanceData = null;

        if (instance == this)
            instance = null;
    }

    public void TickLateUpdate()
    {
        // Recreate compute buffer if needed.
        if (m_QuadInstanceBuffer == null || m_QuadInstanceBuffer.count != m_QuadInstanceData.Length)
        {
            if (m_QuadInstanceBuffer != null)
            {
                m_QuadInstanceBuffer.Release();
                m_QuadInstanceBuffer = null;
            }

            m_QuadInstanceBuffer = new ComputeBuffer(m_QuadInstanceData.Length, 16 + 16 + 16);
            resources.glyphMaterial.SetBuffer("positionBuffer", m_QuadInstanceBuffer);
        }

        if (m_LineInstanceBuffer == null || m_LineInstanceBuffer.count != m_LineInstanceData.Length)
        {
            if (m_LineInstanceBuffer != null)
            {
                m_LineInstanceBuffer.Release();
                m_LineInstanceBuffer = null;
            }

            m_LineInstanceBuffer = new ComputeBuffer(m_LineInstanceData.Length, 16 + 16);
            resources.lineMaterial.SetBuffer("positionBuffer", m_LineInstanceBuffer);
        }

        m_QuadInstanceBuffer.SetData(m_QuadInstanceData, 0, 0, m_NumQuadsUsed);
        m_NumQuadsToDraw = m_NumQuadsUsed;

        m_LineInstanceBuffer.SetData(m_LineInstanceData, 0, 0, m_NumLinesUsed);
        m_NumLinesToDraw = m_NumLinesUsed;

        resources.glyphMaterial.SetVector("scales", new Vector4(
            1.0f / width,
            1.0f / height,
            (float)resources.cellWidth / resources.glyphMaterial.mainTexture.width,
            (float)resources.cellHeight / resources.glyphMaterial.mainTexture.height));

        resources.lineMaterial.SetVector("scales", new Vector4(1.0f / width, 1.0f / height, 1.0f / 1280.0f, 1.0f / 720.0f));

        _Clear();
    }

    public static void SetColor(Color col)
    {
        if (instance == null)
            return;
        instance.m_CurrentColor = col;
    }

    public static void SetOrigin(float x, float y)
    {
        if (instance == null)
            return;
        instance.m_OriginX = x;
        instance.m_OriginY = y;
    }

    public static void Write(float x, float y, char[] buf, int count)
    {
        if (instance == null)
            return;
        for (var i = 0; i < count; i++)
            instance.AddQuad(x + i, y, 1, 1, buf[i], instance.m_CurrentColor);
    }

    public static void Write(float x, float y, string format)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format);
        instance._DrawText(x, y, ref _buf, l);
    }
    public static void Write<T>(float x, float y, string format, T arg)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write<T>(ref _buf, 0, format, arg);
        instance._DrawText(x, y, ref _buf, l);
    }
    public static void Write<T>(Color col, float x, float y, string format, T arg)
    {
        if (instance == null)
            return;
        Color c = instance.m_CurrentColor;
        instance.m_CurrentColor = col;
        var l = StringFormatter.Write(ref _buf, 0, format, arg);
        instance._DrawText(x, y, ref _buf, l);
        instance.m_CurrentColor = c;
    }
    public static void Write<T0, T1>(float x, float y, string format, T0 arg0, T1 arg1)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
        instance._DrawText(x, y, ref _buf, l);
    }
    public static void Write<T0, T1, T2>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2);
        instance._DrawText(x, y, ref _buf, l);
    }

    public static void Write<T0, T1, T2, T3>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2, arg3);
        instance._DrawText(x, y, ref _buf, l);
    }

    void _DrawText(float x, float y, ref char[] text, int length)
    {
        const string hexes = "0123456789ABCDEF";
        Vector4 col = m_CurrentColor;
        int xpos = 0;
        for (var i = 0; i < length; i++)
        {
            if (text[i] == '^' && i < length - 3)
            {
                var r = hexes.IndexOf(text[i + 1]);
                var g = hexes.IndexOf(text[i + 2]);
                var b = hexes.IndexOf(text[i + 3]);
                col.x = (float)(r * 16 + r) / 255.0f;
                col.y = (float)(g * 16 + g) / 255.0f;
                col.z = (float)(b * 16 + b) / 255.0f;
                i += 3;
                continue;
            }
            AddQuad(m_OriginX + x + xpos, m_OriginY + y, 1, 1, text[i], col);
            xpos++;
        }
    }

    public void _DrawRect(float x, float y, float w, float h, Color col)
    {
        AddQuad(m_OriginX + x, m_OriginY + y, w, h, '\0', col);
    }

    void _Clear()
    {
        m_NumQuadsUsed = 0;
        m_NumLinesUsed = 0;
        SetOrigin(0, 0);
    }

    static char[] _buf = new char[1024];

    public void Render()
    {
        resources.lineMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, m_NumLinesToDraw * 6, 1);
        resources.glyphMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, m_NumQuadsToDraw * 6, 1);
    }

    unsafe void AddLine(float x1, float y1, float x2, float y2, Vector4 col)
    {
        if (m_NumLinesUsed >= m_LineInstanceData.Length)
        {
            // Resize
            var newBuf = new LineInstanceData[m_LineInstanceData.Length + 128];
            System.Array.Copy(m_LineInstanceData, newBuf, m_LineInstanceData.Length);
            m_LineInstanceData = newBuf;
        }
        fixed (LineInstanceData* d = &m_LineInstanceData[m_NumLinesUsed])
        {
            d->color = col;
            d->position.x = x1;
            d->position.y = y1;
            d->position.z = x2;
            d->position.w = y2;
        }
        m_NumLinesUsed++;
    }

    public unsafe void AddQuad(float x, float y, float w, float h, char c, Vector4 col)
    {
        if (m_NumQuadsUsed >= m_QuadInstanceData.Length)
        {
            // Resize
            var newBuf = new QuadInstanceData[m_QuadInstanceData.Length + 128];
            System.Array.Copy(m_QuadInstanceData, newBuf, m_QuadInstanceData.Length);
            m_QuadInstanceData = newBuf;
        }

        fixed (QuadInstanceData* d = &m_QuadInstanceData[m_NumQuadsUsed])
        {
            if (c != '\0')
            {
                d->positionAndUV.z = (c - 32) % resources.charCols;
                d->positionAndUV.w = (c - 32) / resources.charCols;
                col.w = 0.0f;
            }
            else
            {
                d->positionAndUV.z = 0;
                d->positionAndUV.w = 0;
            }

            d->color = col;
            d->positionAndUV.x = x;
            d->positionAndUV.y = y;
            d->size.x = w;
            d->size.y = h;
            d->size.z = 0;
            d->size.w = 0;
        }

        m_NumQuadsUsed++;
    }


    float m_OriginX;
    float m_OriginY;
    Color m_CurrentColor = Color.white;

    struct QuadInstanceData
    {
        public Vector4 positionAndUV; // if UV are zero, dont sample
        public Vector4 size; // zw unused
        public Vector4 color;
    }

    struct LineInstanceData
    {
        public Vector4 position; // segment from (x,y) to (z,w)
        public Vector4 color;
    }

    int m_NumQuadsUsed = 0;
    int m_NumLinesUsed = 0;

    ComputeBuffer m_QuadInstanceBuffer;
    ComputeBuffer m_LineInstanceBuffer;
    int m_NumQuadsToDraw = 0;
    int m_NumLinesToDraw = 0;
    QuadInstanceData[] m_QuadInstanceData = new QuadInstanceData[128];
    LineInstanceData[] m_LineInstanceData = new LineInstanceData[128];
}
