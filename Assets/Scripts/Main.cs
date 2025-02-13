using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
class _ : MonoBehaviour
{
    void Start() => Application.targetFrameRate = 120;
    void Update() => Main.Instance.Update();
}



public class Main
{
    private static readonly Main instance = new Main();
    public static Main Instance => instance;

    private InputController inputController = new();
    public Ui ui = new();
    public List<Puyo> puyos = new();

    public PuyoPair puyoPair;
    private Eraser eraser = new Eraser();
    private Main()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 14; y++)
            {
                if (x == 0) NewPuyo(new Vector2(x + 0.5f, y + 0.5f));
                else if (y == 0) NewPuyo(new Vector2(x + 0.5f, y + 0.5f));
                else if (x == 7) NewPuyo(new Vector2(x + 0.5f, y + 0.5f));
            }
        }



    }
    public void Update()
    {
        if (puyoPair == null)
        {
            puyoPair = new PuyoPair(NewPuyo(new Vector2(3.5f, 12.5f)), NewPuyo(new Vector2(3.5f, 13.5f)));
        }

        Vector2 input = inputController.Update();

        if (input == Vector2.up + Vector2.right) puyoPair.Rotation();
        else if (input == Vector2.up) puyoPair.Drop();
        else puyoPair.Move(input);


        puyoPair.Update();

        if (puyoPair.sleepCnt > 30)
        {
            puyoPair = null;

        }

        foreach (Puyo p in Main.Instance.puyos)
        {
            if (puyoPair != null && puyoPair.parent == p) continue;
            if (puyoPair != null && puyoPair.child == p) continue;

            p.Update();
        }

        eraser.Update();



        ui.Update();
    }

    private Puyo NewPuyo(Vector2 position)
    {
        Puyo puyo = new Puyo(position);
        puyos.Add(puyo);
        ui.newGameObject(puyo);
        return puyo;
    }
    public static Puyo Collision(Puyo puyo)
    {
        foreach (var p in Main.Instance.puyos)
        {
            if (puyo == p) continue;
            if (Vector2.SqrMagnitude(puyo.position - p.position) < 1 - 0.001f) return p;
        }
        return null;
    }

}

public class Eraser
{
    public List<Puyo> list = new List<Puyo>();

    int cnt;
    public void Update()
    {

        if (list.Count != 0)
        {
            cnt++;
            if (cnt == 1)
            {
                foreach (Puyo puyo in list)
                {
                    puyo.fire = true;
                }
            }
            if (cnt > 60)
            {
                foreach (Puyo puyo in list)
                {
                    UnityEngine.Object.Destroy(Main.Instance.ui.dict[puyo].transform.gameObject);
                    Main.Instance.puyos.Remove(puyo);
                    Main.Instance.ui.dict.Remove(puyo);
                }

                list.Clear();
            }
            return;
        }

        cnt = 0;

        Puyo[,] array = new Puyo[8, 14];
        foreach (Puyo puyo in Main.Instance.puyos)
        {
            if (Main.Instance.puyoPair != null)
            {
                if (Main.Instance.puyoPair.parent == puyo) continue;
                if (Main.Instance.puyoPair.child == puyo) continue;
            }
            if (puyo.sleepCnt < 60) continue;

            array[(int)puyo.position.x, (int)puyo.position.y] = puyo;
        }

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                if (array[x, y] == null) continue;
                List<Puyo> connected = DFS(array[x, y].color, array, x, y);
                if (connected.Count < 4) continue;
                list.AddRange(connected);
            }
        }


    }


    private List<Puyo> DFS(int color, Puyo[,] array, int x, int y)
    {
        List<Puyo> list = new();
        if (x < 1 || x > 6) return list;
        if (y < 1 || y > 12) return list;
        if (array[x, y] == null) return list;
        Puyo puyo = array[x, y];
        if (color != puyo.color) return list;
        array[x, y] = null;

        list.Add(puyo);
        list.AddRange(DFS(color, array, x + 1, y));
        list.AddRange(DFS(color, array, x - 1, y));
        list.AddRange(DFS(color, array, x, y + 1));
        list.AddRange(DFS(color, array, x, y - 1));
        return list;
    }





}





public class Puyo
{
    public Vector2 position;
    public int sleepCnt;
    public int color = UnityEngine.Random.Range(0, 4);
    public bool fire = false;
    public Puyo(Vector2 position)
    {
        this.position = position;
    }

    public void Update()
    {
        if (position.y == 0.5f) return;

        Vector2 p = position;
        Move(Vector2.down * 0.03f);
        if (p == position) sleepCnt++;
        else sleepCnt = 0;
    }


    public void Move(Vector2 direction)
    {
        position += direction;
        Puyo c = Main.Collision(this);
        if (c == null) return;

        position -= direction;
        Vector2 po = position;
        if (direction.x != 0)
        {
            float f = position.y - c.position.y;
            if (Mathf.Abs(f) < 0.5f) return;
            position = c.position + (f > 0 ? Vector2.up : Vector2.down);
            if (Main.Collision(this) == null) return;
            position = po;


        }

        if (direction.y != 0)
        {
            position = c.position + (direction.y > 0 ? Vector2.down : Vector2.up);
        }
    }

}
public class Rotation
{
    private List<Vector2> list = new List<Vector2>() { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
    public Vector2 Next()
    {
        Vector2 tmp = list[0];
        list.Remove(tmp);
        list.Add(tmp);
        return list[0];
    }
    public Vector2 Get()
    {
        return list[0];
    }

}

public class PuyoPair
{
    public Puyo parent, child;
    public int sleepCnt;
    Rotation rotation = new();
    public PuyoPair(Puyo p, Puyo c)
    {
        parent = p;
        child = c;
    }
    public void Drop()
    {
        for (int i = 0; i < 14; i++)
        {
            Vector2 p = parent.position;
            Move(Vector2.down);
            if (parent.position - p == Vector2.down) continue;
            break;
        }
        sleepCnt = 30;

    }
    public void Update()
    {
        Vector2 p = parent.position;
        Move(Vector2.down * 0.03f);

        if (p == parent.position) sleepCnt++;
        else sleepCnt = 0;
    }
    private void SyncWith(Puyo p)
    {
        if (p == parent) child.position = parent.position + rotation.Get();
        else if (p == child) parent.position = child.position - rotation.Get();
    }

    private void SetPos(Puyo p, Vector2 pos)
    {
        if (p == parent) parent.position = pos;
        else if (p == child) child.position = pos;
        SyncWith(p);
    }
    public void Rotation()
    {
        Vector2 pos = parent.position;

        child.position = pos;
        child.Move(rotation.Next());
        SyncWith(child);
        if (Main.Collision(parent) == null) return;

        rotation.Next();
        parent.position = child.position;
        child.position = pos;
        sleepCnt = 0;
    }

    public void Move(Vector2 vector2)
    {
        Vector2 pos = parent.position;
        if (vector2.normalized == rotation.Get())
        {
            child.Move(vector2);
            SyncWith(child);
            return;
        }
        if (vector2.normalized == -rotation.Get())
        {
            parent.Move(vector2);
            SyncWith(parent);
            return;
        }

        parent.Move(vector2);
        SyncWith(parent);
        if (Main.Collision(child) == null) return;

        SetPos(parent, pos);

        child.Move(vector2);
        SyncWith(child);


    }
}


public class Ui
{
    private GameObject puyo;
    private GameObject canvas;
    public Dictionary<Puyo, RectTransform> dict = new Dictionary<Puyo, RectTransform>();
    public Ui()
    {
        Camera camera = new GameObject("").AddComponent<Camera>();
        camera.orthographic = true;

        canvas = new GameObject("Canvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().worldCamera = camera;
        canvas.transform.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.transform.gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        puyo = new GameObject("puyo");
        Image image = puyo.AddComponent<Image>();
        puyo.transform.SetParent(canvas.transform, false);
        image.rectTransform.sizeDelta = new Vector2(64, 64);
        image.sprite = Resources.Load<Sprite>("Circle");
        image.rectTransform.position = new Vector2(-1024, -1024);
    }
    public void newGameObject(Puyo puyo)
    {
        GameObject gameObject = UnityEngine.Object.Instantiate(this.puyo);

        gameObject.GetComponent<Image>().color = Color.HSVToRGB(puyo.color * 0.2f, 0.5f, 0.8f);
        dict.Add(puyo, gameObject.GetComponent<RectTransform>());
        gameObject.transform.SetParent(canvas.transform, false);
        dict[puyo].anchoredPosition = puyo.position;
    }
    public void Update()
    {
        foreach (var puyo in dict)
        {
            puyo.Value.anchoredPosition = puyo.Key.position * 64 + new Vector2(-480, -270);


            if (puyo.Key.fire)
            {
                puyo.Value.transform.localScale = new Vector2(1, 1.5f);
                continue;
            }

            if (puyo.Key.sleepCnt > 60) continue;
            {
                float f = puyo.Key.sleepCnt / 60f;
                f = Mathf.Sin(Mathf.PI * f) * 0.25f;
                puyo.Value.transform.localScale = new Vector2(1 + f, 1);

                f = puyo.Key.sleepCnt / 60f;
                f = Mathf.Sin(Mathf.PI * f);
                puyo.Value.anchoredPosition = puyo.Value.anchoredPosition + Vector2.down * f * 16;
            }


        }

    }


}

public class InputController
{
    public Vector2 Update() =>
        Input.GetKeyDown(KeyCode.W) ? Vector2.up :
        Input.GetKeyDown(KeyCode.A) ? Vector2.left :
        Input.GetKeyDown(KeyCode.S) ? Vector2.down :
        Input.GetKeyDown(KeyCode.D) ? Vector2.right :
        Input.GetKeyDown(KeyCode.Space) ? Vector2.up + Vector2.right :

        Vector2.zero;
}
