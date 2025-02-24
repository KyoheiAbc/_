using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class _ : MonoBehaviour
{
    Game game;
    void Start()
    {
        Application.targetFrameRate = 30;
        game = new Game();
    }
    void Update() => game.Update();
}

public class Game
{
    InputController inputController;
    Render render;
    PuyoPair puyoPair = null;
    List<Puyo> puyos = new List<Puyo>();
    Eraser eraser = new Eraser();

    public Game()
    {
        inputController = new InputController();
        render = new Render();

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 0 || x == 7 || y == 0 || y == 15)
                {
                    puyos.Add(new Puyo(new Vector2(x + 0.5f, y + 0.5f)));
                    puyos[puyos.Count - 1].fixedPuyo = true;
                }
            }
        }

    }
    private bool NeedNewPuyo()
    {
        if (puyoPair != null) return false;

        foreach (var puyo in puyos)
        {

            if (puyo.position.x < 1) continue;
            if (puyo.position.y < 1) continue;
            if (puyo.position.x > 7) continue;
            if (puyo.position.y > 15) continue;

            if (puyo.cnt < 15) return false;

            if (eraser.cnt > 0) return false;
        }

        return true;
    }

    public void Update()
    {
        Vector2 direction = inputController.Update();

        puyos.Sort((a, b) => b.position.y.CompareTo(a.position.y));

        if (NeedNewPuyo())
        {
            puyoPair = new PuyoPair(new Vector2(3.5f, 12.5f));
        }


        if (direction == Vector2.right + Vector2.down)
        {
            if (puyoPair != null)
            {
                puyoPair.Rotate(puyos);
            }
        }
        // else if (direction == Vector2.right * 2)
        // {
        //     puyos.Add(new Puyo(new Vector2(Random.Range(-5, 5), Random.Range(-5, 5))));
        // }
        else if (direction != Vector2.zero)
        {
            if (puyoPair != null)
            {
                puyoPair.Move(direction, puyos);
            }
        }

        if (puyoPair != null)
        {

            if (puyoPair.cnt == 30)
            {
                puyos.Add(puyoPair.parent);
                puyos.Add(puyoPair.child);
                puyoPair = null;
            }
            else
            {
                puyoPair.Update(puyos);
            }
        }

        for (int i = puyos.Count - 1; i >= 0; i--)
        {
            puyos[i].Update(puyos);
        }

        eraser.Update(puyos);


        render.Update(puyoPair, puyos);
    }
}

public static class Public
{
    public static Puyo CheckCollision(Puyo puyo, List<Puyo> puyos)
    {
        foreach (var other in puyos)
        {
            if (puyo == other)
            {
                continue;
            }
            if (Vector2.Distance(puyo.position, other.position) < 1)
            {
                return other;
            }
        }
        return null;
    }
}
public class InputController
{
    public Vector2 Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            return Vector2.up;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            return Vector2.down;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            return Vector2.left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            return Vector2.right;
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            return Vector2.right + Vector2.down;
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            return 2 * Vector2.right;
        }

        return Vector2.zero;
    }
}
public class Puyo
{
    public bool erasable = false;
    public int color;
    public Vector2 position;
    public int cnt = 0;
    public bool fixedPuyo = false;
    public Puyo(Vector2 position)
    {
        this.position = position;
        this.color = UnityEngine.Random.Range(0, 3);
    }
    public void Update(List<Puyo> puyos)
    {
        if (fixedPuyo) return;
        if (Vector2Move(Vector2.down * 0.3f, puyos) != Vector2.down * 0.3f)
        {
            cnt++;
        }
        else
        {
            cnt = 0;
        }
    }


    public Vector2 Vector2Move(Vector2 direction, List<Puyo> puyos)
    {
        Vector2 originalPosition = position;
        Move(direction, puyos);
        return position - originalPosition;
    }
    public void Move(Vector2 direction, List<Puyo> puyos)
    {
        Vector2 originalPosition = position;
        position += direction;

        Puyo collidedPuyo = Public.CheckCollision(this, puyos);
        if (collidedPuyo == null) return;

        position = originalPosition;

        if (direction.y != 0)
        {
            position = collidedPuyo.position + (direction.y > 0 ? Vector2.down : Vector2.up);
            return;
        }

        float verticalDifference = position.y - collidedPuyo.position.y;
        if (Mathf.Abs(verticalDifference) < 0.5f) return;
        position = collidedPuyo.position + (verticalDifference > 0 ? Vector2.up : Vector2.down);

        if (Public.CheckCollision(this, puyos) == null) return;
        position = originalPosition;
    }
}

public class Rotation
{
    List<Vector2> directions = new List<Vector2> { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

    public Vector2 Get() => directions[0];
    public Vector2 Next()
    {
        Vector2 currentDirection = directions[0];
        directions.RemoveAt(0);
        directions.Add(currentDirection);
        return directions[0];
    }
}
public class PuyoPair
{
    public Puyo parent, child;
    Rotation rotation;
    public int cnt = 0;
    public PuyoPair(Vector2 position)
    {
        rotation = new Rotation();
        parent = new Puyo(position);
        child = new Puyo(position + rotation.Get());
    }


    private void Sync(Puyo p)
    {
        if (p == parent) child.position = parent.position + rotation.Get();
        else parent.position = child.position - rotation.Get();
    }
    public void Rotate(List<Puyo> puyos)
    {
        Vector2 originalPosition = parent.position;


        child.position = parent.position;
        child.Move(rotation.Next(), puyos);
        Sync(child);
        if (Public.CheckCollision(parent, puyos) == null) return;

        rotation.Next();

        child.position = originalPosition;
        Sync(child);
    }
    public void Update(List<Puyo> puyos)
    {
        if (Vector2Move(Vector2.down * 0.1f, puyos) != Vector2.down * 0.1f)
        {
            cnt++;
        }
        else
        {
            cnt = 0;
        }
    }

    public Vector2 Vector2Move(Vector2 direction, List<Puyo> puyos)
    {
        Vector2 originalPosition = parent.position;
        Move(direction, puyos);
        return parent.position - originalPosition;
    }

    public void Move(Vector2 direction, List<Puyo> puyos)
    {
        Vector2 originalPosition = parent.position;


        parent.Move(direction, puyos);
        Sync(parent);
        if (Public.CheckCollision(child, puyos) == null) return;

        parent.position = originalPosition;
        Sync(parent);

        child.Move(direction, puyos);
        Sync(child);
        if (Public.CheckCollision(parent, puyos) == null) return;

        parent.position = originalPosition;
        Sync(parent);
    }
}

public class Eraser
{
    List<Puyo> list = new List<Puyo>();
    public int cnt = 0;

    public void Update(List<Puyo> puyos)
    {

        if (list.Count > 0)
        {
            cnt++;
            if (cnt < 30) return;
            foreach (var puyo in list)
            {
                puyos.Remove(puyo);
            }
            list.Clear();
            return;
        }

        cnt = 0;



        Puyo[,] array = new Puyo[8, 14];
        foreach (Puyo puyo in puyos)
        {
            if (puyo.position.x < 1) continue;
            if (puyo.position.y < 1) continue;
            if (puyo.position.x > 7) continue;
            if (puyo.position.y > 15) continue;

            if (puyo.cnt < 15) return;
            array[(int)puyo.position.x, (int)puyo.position.y] = puyo;
        }

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                if (array[x, y] == null) continue;
                List<Puyo> connected = DFS(array[x, y].color, array, x, y);
                if (connected.Count < 3) continue;
                list.AddRange(connected);
                cnt = 1;
                foreach (var puyo in connected)
                {
                    puyo.erasable = true;
                }
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

public class Render
{
    GameObject puyoGameObject;
    Dictionary<Puyo, Transform> puyoTransforms = new Dictionary<Puyo, Transform>();
    public Render()
    {
        Camera camera = new GameObject().AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10;
        camera.transform.position = new Vector3(4, 8, -1);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        puyoGameObject = new GameObject();
        puyoGameObject.transform.position = new Vector3(-256, -256, 0);
        puyoGameObject.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Circle");
    }

    public void Update(PuyoPair puyoPair, List<Puyo> puyos)
    {
        List<Puyo> list = new List<Puyo>(puyos);
        if (puyoPair != null)
        {
            list.Add(puyoPair.parent);
            list.Add(puyoPair.child);
        }

        List<Puyo> keys = new List<Puyo>(puyoTransforms.Keys);
        for (int i = keys.Count - 1; i >= 0; i--)
        {
            Puyo puyo = keys[i];
            if (!list.Contains(puyo))
            {
                GameObject.Destroy(puyoTransforms[puyo].gameObject);
                puyoTransforms.Remove(puyo);
            }
        }

        foreach (var puyo in list)
        {
            if (!puyoTransforms.ContainsKey(puyo))
            {
                Transform transform = GameObject.Instantiate(puyoGameObject).transform;
                transform.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(puyo.color / 5f, 0.5f, 0.8f);
                puyoTransforms.Add(puyo, transform);
            }
            puyoTransforms[puyo].position = puyo.position;
            if (puyo.erasable)
            {
                puyoTransforms[puyo].localScale = new Vector3(1, 1.5f, 1);
                continue;
            }
            if (puyo.cnt > 0)
            {
                float f = puyo.cnt / 15f;
                if (f > 1) f = 1;
                f = Mathf.Sin(f * Mathf.PI) * 0.3f;
                puyoTransforms[puyo].localScale = new Vector3(1 + f, 1 - f, 1);
                puyoTransforms[puyo].position += new Vector3(0, -f, 0);
            }
        }

    }
}
