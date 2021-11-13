using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class Grid : MonoBehaviour
{
    // we have 5 colors
    // 0 tank -> blue
    // 1 warrior -> red
    // 2 assassin -> yellow
    // 3 support -> green
    // 4 marksman -> purple

    // 7x7 grid for test purposes
    private int[][] matrix =
    {
        new int[]{ 1,3,4,2,3,0,1 },
        new int[]{ 4,0,0,0,3,1,1 },
        new int[]{ 4,2,1,2,2,2,4 },
        new int[]{ 0,1,0,1,4,0,3 },
        new int[]{ 0,2,3,2,4,3,3 },
        new int[]{ 1,2,0,1,0,3,3 },
        new int[]{ 1,2,2,4,3,2,1 }
    };

    private GridElement[][] gridElements;
    public GridElement gridElementPrefab;
    public Camera mainCam;
    public float padding = .1f;
    public float margin = 1f;
    private float camWidth;
    private float camHeight;
    private RaycastHit2D[] results = new RaycastHit2D[3];
    private Vector3 vmargin;
    public float height;
    public float width;

    public enum State
    {
        Animating = 0,
        CanTap = 1,
    }

    public State state = State.CanTap;
    public bool initialized = false;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        Layout(matrix);
        camHeight = mainCam.orthographicSize * 2;
        camWidth = camHeight * mainCam.aspect;
        state = State.CanTap;
    }

    private void Layout(int[][] matrix)
    {
        gridElements = new GridElement[matrix.Length][];

        // each cell is a 1 unit
        int row = matrix.Length;
        int col = matrix[0].Length;

        // first element doest have a padding
        width = col + (col - 1) * padding;
        height = row + (row - 1) * padding;

        // margin is distance between containers to the page layout
        vmargin = new Vector3(width * -.5f, height * +.5f, 0f);

        // we are starting to place from Origin to +x and -y direction.
        for(int i = 0; i < matrix.Length; i++)
        {
            gridElements[i] = new GridElement[matrix[i].Length];

            for(int j = 0; j < matrix[i].Length; j++)
            {
                int m = matrix[i][j];
                float xPos = j;
                float yPos = -i;
                //float yPos = matrix.Length - i;
                // padding is distance between the elements
                Vector3 vpadding = new Vector3(xPos * padding, yPos * padding, 0f);
                Color color = GetColorCode(m);
                GridElement element = Instantiate(gridElementPrefab);
                element.colorCode = m;
                element.row = i;
                element.col = j;
                element.color = color;
                element.spriteRenderer.color = color;
                element.transform.position = new Vector3(xPos, yPos, 0f) + vpadding + vmargin;
                //element.transform.position = new Vector3(j + j * .1f, (matrix.Length - i) + ((matrix.Length - i) * .1f), 0f);
                gridElements[i][j] = element;
            }
        }
    }

    private bool RangeCheck<T>(T[][] matrix, int row, int col)
    {
        if(matrix.Length > row && row > -1 && matrix[row].Length > col && col > -1)
            return true;

        return false;
    }

    public LinkedList<GridElement> FloodFillFunction(GridElement[][] matrix, GridElement gridElement)
    {
        float time = Time.realtimeSinceStartup;

        Queue<GridElement> queue = new Queue<GridElement>(50); // 50 because we have 7 x 7 grid for now.
        queue.Enqueue(gridElement);
        LinkedList<GridElement> elements = new LinkedList<GridElement>();
        int colorCode = gridElement.colorCode;
        int[][] visited = new int[matrix.Length][];
        for(int i = 0; i < matrix.Length; i++)
        {
            visited[i] = new int[matrix[i].Length];
        }

        while(queue.Count > 0)
        {
            var element = queue.Dequeue();
            int r = element.row;
            int c = element.col;

            // move down
            if(RangeCheck(matrix, r + 1, c)
                && matrix[r + 1][c].colorCode == colorCode
                && visited[r + 1][c] == 0)
            {
                queue.Enqueue(matrix[r + 1][c]);
                elements.AddLast(matrix[r + 1][c]);
                visited[r + 1][c] = 1;
            }

            // move up
            if(RangeCheck(matrix, r - 1, c)
                && matrix[r - 1][c].colorCode == colorCode
                && visited[r - 1][c] == 0)
            {
                queue.Enqueue(matrix[r - 1][c]);
                elements.AddLast(matrix[r - 1][c]);
                visited[r - 1][c] = 1;
            }

            // move right
            if(RangeCheck(matrix, r, c + 1)
                && matrix[r][c + 1].colorCode == colorCode
                && visited[r][c + 1] == 0)
            {
                queue.Enqueue(matrix[r][c + 1]);
                elements.AddLast(matrix[r][c + 1]);
                visited[r][c + 1] = 1;
            }

            // move left
            if(RangeCheck(matrix, r, c - 1)
                && matrix[r][c - 1].colorCode == colorCode
                && visited[r][c - 1] == 0)
            {
                queue.Enqueue(matrix[r][c - 1]);
                elements.AddLast(matrix[r][c - 1]);
                visited[r][c - 1] = 1;
            }
        }

        //Debug.Log(Time.realtimeSinceStartup - time);

        return elements;
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0) && state == State.CanTap)
        {
            var mousePosition = Input.mousePosition;
            Vector3 origin = Camera.main.ScreenToWorldPoint(mousePosition);

            var len = Physics2D.OverlapPoint(origin, 1 << 6);

            if(len != null && len.transform.CompareTag("GridElement"))
            {
                var element = len.GetComponent<GridElement>();
                var elements = FloodFillFunction(gridElements, element);

                if(elements.Count < 2)
                {
                    return;
                }

                // collect the cells
                foreach(var item in elements)
                {
                    item.gameObject.SetActive(false);
                    item.state = GridElement.State.Collected;
                }

                state = State.Animating;

                // shift the grid
                Shift();

                // fill the empty spaces.
                Fill();
            }
        }
    }

    // vertical search spawn new blocks
    public void Fill()
    {
        // we are starting to place from Origin to +x and -y direction.
        int row = gridElements.Length;
        int col = gridElements[0].Length;
        LinkedList<GridElement> elements = new LinkedList<GridElement>();
        //int colorCode = Random.Range(0, 5);
        //Color color = GetColorCode(colorCode);

        for(int j = 0; j < col; j++)
        {
            for(int i = gridElements.Length - 1; i >= 0; i--)
            {
                var element = gridElements[i][j];
                if(element.state == GridElement.State.Collected)
                {
                    int colorCode = Random.Range(0, 5);
                    Color color = GetColorCode(colorCode);
                    Vector3 position = element.transform.position;
                    position.y = height * .75f;
                    element.transform.position = position;
                    element.colorCode = colorCode;
                    element.color = color;
                    element.spriteRenderer.color = color;
                    elements.AddLast(element);
                }
            }

            // animate col by col
            float delay = .5f;
            foreach(var item in elements)
            {
                delay += .2f;

                BasicTween.Instance.DelayedCall(delay, () =>
                {
                    item.gameObject.SetActive(true);
                    item.state = GridElement.State.Placed;
                });
                Vector3 pos = FindPosition(item.row, item.col);
                BasicTween.Instance.AppendPosition(item.transform, pos, .5f, delay);
            }

            BasicTween.Instance.DelayedCall(delay + .5f, () => { state = State.CanTap; });

            elements.Clear();
        }
    }

    // vertical search and shift block one by one.
    public void Shift()
    {
        // we are starting to place from Origin to +x and -y direction.
        int row = gridElements.Length;
        int col = gridElements[0].Length;

        LinkedList<GridElement> elements = new LinkedList<GridElement>();

        for(int j = 0; j < col; j++)
        {
            for(int i = gridElements.Length - 1; i >= 0; i--)
            {
                var element = gridElements[i][j];
                if(element.state == GridElement.State.Collected)
                {
                    continue;
                }
                var target = FindMyPlace(gridElements, element);

                if(target == null)
                    continue;

                gridElements[i][j] = target;
                gridElements[target.row][target.col] = element;

                int tempCol = element.col;
                int tempRow = element.row;

                element.col = target.col;
                element.row = target.row;
                element.fromCol = tempCol;
                element.fromRow = tempRow;
                element.state = GridElement.State.Animating;

                target.col = tempCol;
                target.row = tempRow;

                if(!elements.Contains(element))
                {
                    elements.AddLast(element);
                }
            }

            // animate col by col
            float delay = .05f;

            foreach(var item in elements)
            {
                Vector3 pos = FindPosition(item.row, item.col);
                delay += .05f;
                BasicTween.Instance.AppendPosition(item.transform, pos, .5f, delay);
            }

            elements.Clear();
        }
    }

    public Vector3 FindPosition(int row, int col)
    {
        float xPos = col;
        float yPos = -row;
        Vector3 vpadding = new Vector3(xPos * padding, yPos * padding, 0f);
        return new Vector3(xPos, yPos, 0f) + vpadding + vmargin;
    }

    public GridElement FindMyPlace(GridElement[][] matrix, GridElement gridElement)
    {
        Queue<GridElement> queue = new Queue<GridElement>(50); // 50 because we have 7 x 7 grid for now.
        queue.Enqueue(gridElement);
        LinkedList<GridElement> elements = new LinkedList<GridElement>();
        int[][] visited = new int[matrix.Length][];
        for(int i = 0; i < matrix.Length; i++)
        {
            visited[i] = new int[matrix[i].Length];
        }

        GridElement target = null;

        while(queue.Count > 0)
        {
            var element = queue.Dequeue();
            int r = element.row;
            int c = element.col;

            // move down
            if(RangeCheck(matrix, r + 1, c)
                && matrix[r + 1][c].state == GridElement.State.Collected)
            {
                queue.Enqueue(matrix[r + 1][c]);
                target = matrix[r + 1][c];
            }
        }

        return target;
    }

    public Color GetColorCode(int id)
    {
        switch(id)
        {
            case 0: // blue
                return new Color32(0, 153, 255, 255);

            case 1: // red
                return new Color32(255, 0, 0, 255);

            case 2: // yellow
                return new Color32(255, 247, 0, 255);

            case 3: // green
                return new Color32(0, 177, 6, 255);

            case 4: // purple
                return new Color(197, 0, 255, 255);
            default:
                return Color.white;
        }
    }
}

