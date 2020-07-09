using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class Desk : MonoBehaviour
{
    public int width, hight;
    

    [SerializeField] private GameObject[] sphereList;
    public List<Sphere> allSphereList = new List<Sphere>();

    public static Desk instance;

    public bool isSwapping;
    Sphere lastSphere;
    Sphere sphere1, sphere2;
    Vector3 sphere1StartPos, sphere1EndPos, sphere2StartPos, sphere2EndPos;
    float dist;

    bool turnChecked, createSphereDown;
    public bool gravityDown = true;
    [SerializeField]
    float gravityMax, gravityMin,swapSpeed;
    [SerializeField]GameObject blockUpObj, blockDownObj;

   

    private void Awake()
    {
        instance = this;
        
    }

    private void Start()
    {
        FillDesk();


        GameMode();
        StartCoroutine(DeskCheck());
        StartCoroutine(Gravity());
        
    }
    public void RefreshDesk()
    {
        
        StopAllCoroutines();
        gravityDown = true;
        allSphereList.Clear();
        
        GameObject[] destroyObjects = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject g in destroyObjects)
        {
            Destroy(g);
        }


        GameMode();
        FillDesk();
        StartCoroutine(DeskCheck());
        StartCoroutine(Gravity());

    }
    private void GameMode()
    {
        if (PlayerPrefs.GetInt("RandomGrid") == 1)
        {
            RandomGridDraw.instance.DrawGrid();
        }
        else if (PlayerPrefs.GetInt("FigureGrid") == 1)
        {
            FigureDrawGrid.instance.DrawGrid();
        }
    }
 #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < hight; j++)
            {
                Gizmos.DrawWireCube(new Vector3(transform.position.x + i, transform.position.y + j, 0), new Vector3(1, 1, 1));
            }
        }
    }
#endif
    private void FillDesk()
    {
        gravityDown = true;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < hight; j++)
            {
                int randomIndex = Random.Range(0, sphereList.Length);
                GameObject newSphere = Instantiate(sphereList[randomIndex], new Vector3(transform.position.x + i, transform.position.y + j, 0), Quaternion.identity) as GameObject;
                allSphereList.Add(newSphere.GetComponent<Sphere>());
                newSphere.transform.parent = this.transform;
                newSphere.transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }

    public void SwitchPhysics(bool on)
    {
        for (int i = 0; i < allSphereList.Count; i++)
        {
            allSphereList[i].GetComponent<Rigidbody>().isKinematic = on;
        }
    }

    public void SwapSphere(Sphere currentSphere)
    {
        if (lastSphere == null)
        {
            lastSphere = currentSphere;
        }
        else if (lastSphere == currentSphere)
        {
            lastSphere = null;
        }
        else
        {
            if (lastSphere.CheckIfNeighbour(currentSphere))
            {
                sphere1 = lastSphere;
                sphere2 = currentSphere;

                sphere1StartPos = lastSphere.transform.position;
                sphere1EndPos = currentSphere.transform.position;

                sphere2StartPos = currentSphere.transform.position;
                sphere2EndPos = lastSphere.transform.position;

                StartCoroutine(SwapSphere());
            }
            else
            {
                lastSphere.Selector();
                lastSphere = currentSphere;
            }
        }
    }
    IEnumerator SwapSphere()
    {
        if (isSwapping)
        {
            yield break;
        }

        isSwapping = true;

        SwitchPhysics(true);
        while (MoveToSwapPosition(sphere1, sphere1EndPos) && MoveToSwapPosition(sphere2, sphere2EndPos)) { yield return null; }

        sphere1.CleaAllMatches();
        sphere2.CleaAllMatches();

        while (!turnChecked) { yield return null; }
        if (!sphere1.matchFound && !sphere2.matchFound)
        {
            while (MoveToSwapPosition(sphere1, sphere1StartPos) && MoveToSwapPosition(sphere2, sphere2StartPos)) { yield return null; }
        }
        turnChecked = false;


        isSwapping = false;

        SwitchPhysics(false);
        lastSphere = null;
        sphere1.Selector();
        sphere2.Selector();
    }

    bool MoveToSwapPosition(Sphere s, Vector3 swapGoal)
    {
        return s.transform.position != (s.transform.position = Vector3.MoveTowards(s.transform.position, swapGoal, swapSpeed * Time.deltaTime));
    }

    public void ReportTurnDone()
    {
        turnChecked = true;
    }

    public bool CheckIfDeskMoving()
    {
        for (int i = 0; i < allSphereList.Count; i++)
        {
            if (allSphereList[i].transform.localPosition.y > 10f)
            {
                return true;
            }
            if (allSphereList[i].GetComponent<Rigidbody>().velocity.y > 0.1f)
            {
                return true;
            }
        }
        return false;
    }
    IEnumerator DeskCheck()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            if (!isSwapping && !CheckIfDeskMoving())
            {
                for (int i = 0; i < allSphereList.Count; i++)
                {
                    allSphereList[i].CleaAllMatches();
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
    public void CreateNewSphere(Sphere s, Vector3 pos)
    {
        allSphereList.Remove(s);
        int randSphere = Random.Range(0, sphereList.Length);

        if (createSphereDown)
        {
            dist = +10f;
        }
        else
        {
            dist = -10f;
        }

        GameObject newSphere = Instantiate(sphereList[randSphere], new Vector3(pos.x, pos.y + dist, pos.z), Quaternion.identity);
        allSphereList.Add(newSphere.GetComponent<Sphere>());
        newSphere.transform.parent = transform;
        newSphere.transform.localScale = new Vector3(1, 1, 1);

    }
    IEnumerator Gravity()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            if (!isSwapping && !CheckIfDeskMoving())
            {

                if (gravityDown)
                {
                    createSphereDown = true;
                    Physics.gravity = new Vector3(0, gravityMin, 0);
                    blockDownObj.GetComponent<BoxCollider>().enabled = true;
                    blockUpObj.GetComponent<BoxCollider>().enabled = false;
                }
                else
                {
                    createSphereDown = false;
                    Physics.gravity = new Vector3(0, gravityMax, 0);
                    blockDownObj.GetComponent<BoxCollider>().enabled = false;
                    blockUpObj.GetComponent<BoxCollider>().enabled = true;
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
    public void DestroyById(int id)
    {
        for (int i = 0; i < allSphereList.Count; i++)
        {
            if(allSphereList[i].sphereId==id)
            allSphereList[i].matchFound=true;
        }
    }
}
