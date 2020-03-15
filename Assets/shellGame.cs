using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class shellGame : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable button;
    public KMSelectable[] cupButtons;
    public Transform[] cups;
    public Transform[] pivots;
    public GameObject[] highlights;
    public Transform pearl;
    public Transform defaultPosition;

    private int startingCup;
    private int endingCup;
    private int tableRule;
    private int solution;
    private int[] rotations = new int[10];
    private static readonly int[][] table = new int[7][] {
        new int[3] { 0, 1, 2 },
        new int[3] { 3, 2, 1 },
        new int[3] { 1, 0, 2 },
        new int[3] { 1, 3, 0 },
        new int[3] { 0, 2, 3 },
        new int[3] { 3, 2, 0 },
        new int[3] { 2, 1, 0 }
    };

    private static readonly int[][] cupsToRotate = new int[3][] {
        new int[2] { 0, 1 },
        new int[2] { 0, 2 },
        new int[2] { 1, 2 }
    };
    private static readonly Vector3[] defaultCupPositions = new[] { new Vector3(-0.056f, 0.0347f, 0.0187428f), new Vector3(0, 0.0347f, 0.0187428f), new Vector3(0.056f, 0.0347f, 0.0187428f) };
    private static readonly string[] positionNames = new string[3] { "left", "middle", "right" };
    private bool hasRotated;
    private bool cantPress;
    private bool cantPressCup = true;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { PressButton(); return false; };
        foreach (KMSelectable cup in cupButtons)
            cup.OnInteract += delegate () { PressCup(cup); return false; };
    }

    void Start()
    {
        tableRule = CalculateTableRule();
        Debug.LogFormat("[Shell Game #{0}] Using row {1}.", moduleId, CalculateTableRule() + 1);
    }

    IEnumerator StageTwo()
    {
        yield return null;
        endingCup = Array.IndexOf(cups, cups.Where(c => c.GetComponentsInChildren<Transform>(false).Any(x => x.name == "pearl")).First());
        foreach (GameObject highlight in highlights)
            highlight.SetActive(true);
        Debug.LogFormat("[Shell Game #{0}] After shuffling, the pearl is under the {1} cup.", moduleId, positionNames[endingCup]);
        solution = table[tableRule][endingCup];
        if (solution != 3)
        {
            Debug.LogFormat("[Shell Game #{0}] The pearl is actually under the {1} cup.", moduleId, positionNames[solution]);
            pearl.SetParent(cups[solution], false);
        }
        else
        {
            Debug.LogFormat("[Shell Game #{0}] The module stole the pearl! Don't touch any cups.", moduleId);
            pearl.gameObject.SetActive(false);
        }
        cantPressCup = false;
        yield return new WaitForSeconds(5f);
        if (solution != 3)
        {
            module.HandleStrike();
            cantPress = false;
            cantPressCup = true;
            hasRotated = false;
            Debug.LogFormat("[Shell Game #{0}] You didn't pick any cup. Strike!", moduleId);
        }
        else
        {
            Debug.LogFormat("[Shell Game #{0}] You didn't pick any cup. That was correct. Module solved!", moduleId);
            StartCoroutine(Solve());
        }
    }

    void PressButton()
    {
        button.AddInteractionPunch(1f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved || cantPress)
            return;
        startingCup = rnd.Range(0, 3);
        pearl.gameObject.SetActive(true);
        pearl.SetParent(cups[startingCup], false);
        Debug.LogFormat("[Shell Game #{0}] The pearl is under the {1} cup.", moduleId, positionNames[startingCup]);
        pearl.SetParent(defaultPosition, true);
        for (int i = 0; i < 10; i++)
            rotations[i] = rnd.Range(0, 3);
        StartCoroutine(RiseCups());
    }

    void PressCup(KMSelectable cup)
    {
        if (moduleSolved || cantPressCup)
            return;
        var ix = Array.IndexOf(cupButtons, cup);
        if (ix != solution)
        {
            module.HandleStrike();
            StopAllCoroutines();
            hasRotated = false;
            cantPress = false;
            cantPressCup = true;
            Debug.LogFormat("[Shell Game #{0}] You chose the {1} cup. That was incorrect. Strike!", moduleId, positionNames[ix]);
        }
        else
        {
            StopAllCoroutines();
            Debug.LogFormat("[Shell Game #{0}] You chose the {1} cup. That was correct. Module solved!", moduleId, positionNames[ix]);
            StartCoroutine(Solve());
        }
    }

    IEnumerator Solve()
    {
        yield return null;
        moduleSolved = true;
        module.HandlePass();
        foreach (GameObject highlight in highlights)
            highlight.SetActive(false);
        pearl.SetParent(defaultPosition, true);
        if (solution != 3)
        {
            var elapsed = 0f;
            var duration = .5f;
            while (elapsed < duration)
            {
                cups[solution].localPosition = new Vector3(
                    defaultCupPositions[solution].x,
                    Mathf.Lerp(defaultCupPositions[solution].y, .0765f, elapsed / duration),
                    defaultCupPositions[solution].z
                );
                yield return null;
                elapsed += Time.deltaTime;
            }
            cups[solution].localPosition = new Vector3(defaultCupPositions[solution].x, .0765f, defaultCupPositions[solution].z);
            yield return new WaitForSeconds(.25f);
            elapsed = 0f;
            duration = .5f;
            while (elapsed < duration)
            {
                cups[solution].localPosition = new Vector3(
                    defaultCupPositions[solution].x,
                    .0765f,
                    Mathf.Lerp(defaultCupPositions[solution].z, .0535f, elapsed / duration)
                );
                yield return null;
                elapsed += Time.deltaTime;
            }
            cups[solution].localPosition = new Vector3(defaultCupPositions[solution].x, .0765f, .0535f);
            yield return new WaitForSeconds(.25f);
            elapsed = 0f;
            duration = .5f;
            while (elapsed < duration)
            {
                cups[solution].localPosition = new Vector3(
                    defaultCupPositions[solution].x,
                    Mathf.Lerp(.0765f, defaultCupPositions[solution].y, elapsed / duration),
                    .0535f
                );
                yield return null;
                elapsed += Time.deltaTime;
            }
            audio.PlaySoundAtTransform("tap", cups[solution]);
        }
    }

    IEnumerator RiseCups()
    {
        foreach (GameObject highlight in highlights)
            highlight.SetActive(false);
        cantPress = true;
        cantPressCup = true;
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            for (int i = 0; i < cups.Length; i++)
                cups[i].localPosition = new Vector3(
                    defaultCupPositions[i].x,
                    Mathf.Lerp(defaultCupPositions[i].y, .0765f, elapsed / duration),
                    defaultCupPositions[i].z
                );
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int i = 0; i < cups.Length; i++)
            cups[i].localPosition = new Vector3(defaultCupPositions[i].x, .0765f, defaultCupPositions[i].z);
        yield return new WaitForSeconds(2f);
        elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < cups.Length; i++)
                cups[i].localPosition = new Vector3(
                    defaultCupPositions[i].x,
                    Mathf.Lerp(.0765f, defaultCupPositions[i].y, elapsed / duration),
                    defaultCupPositions[i].z
                );
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int i = 0; i < cups.Length; i++)
            cups[i].localPosition = defaultCupPositions[i];
        pearl.SetParent(cups[startingCup], true);
        if (!hasRotated)
            StartCoroutine(RotateCups());
    }

    IEnumerator RotateCups()
    {
        for (int i = 0; i < 10; i++)
        {
            foreach (int ix in cupsToRotate[rotations[i]])
                cups[ix].SetParent(pivots[rotations[i]], true);
            var endRotation = Quaternion.Euler(0f, 180f, 0f);
            var elapsed = 0f;
            var duration = .5f;
            audio.PlaySoundAtTransform("slide" + rnd.Range(1, 6), defaultPosition);
            while (elapsed < duration)
            {
                pivots[rotations[i]].localRotation = Quaternion.Slerp(Quaternion.identity, endRotation, elapsed / duration);
                yield return null;
                elapsed += Time.deltaTime;
            }
            pivots[rotations[i]].localRotation = endRotation;
            foreach (int ix in cupsToRotate[rotations[i]])
                cups[ix].SetParent(defaultPosition, true);
            pivots[rotations[i]].localRotation = Quaternion.identity;
            var t = cups[cupsToRotate[rotations[i]][0]];
            cups[cupsToRotate[rotations[i]][0]] = cups[cupsToRotate[rotations[i]][1]];
            cups[cupsToRotate[rotations[i]][1]] = t;
            var b = cupButtons[cupsToRotate[rotations[i]][0]];
            cupButtons[cupsToRotate[rotations[i]][0]] = cupButtons[cupsToRotate[rotations[i]][1]];
            cupButtons[cupsToRotate[rotations[i]][1]] = b;
            yield return new WaitForSeconds(.25f);
        }
        hasRotated = true;
        StartCoroutine(StageTwo());
    }

    void Update()
    {
        if (bomb.GetStrikes() == 1)
        {
            if (tableRule > 2)
            {
                tableRule = 2;
                Debug.LogFormat("[Shell Game #{0}] The strikes rule is now true. Use that row.", module);
            }
        }
        else if (bomb.GetStrikes() > 1 && tableRule == 2)
        {
            Debug.LogFormat("[Shell Game #{0}] The strikes rule is no longer true. Use row {1}.", moduleId, CalculateTableRule() + 1);
            tableRule = CalculateTableRule();
        }
    }

    int CalculateTableRule()
    {
        var ser = bomb.GetSerialNumber();
        if (bomb.GetOnIndicators().Contains("BOB"))
            return 0;
        else if (bomb.GetOnIndicators().Any(ind => ser.Intersect(ind).Any()) || bomb.GetOffIndicators().Any(ind => ser.Intersect(ind).Any()))
            return 1;
        else if (bomb.GetPortCount(Port.Serial) > bomb.GetPortCount(Port.RJ45) + bomb.GetPortCount(Port.StereoRCA))
            return 3;
        else if (bomb.GetBatteryCount() == bomb.GetSerialNumberNumbers().Last())
            return 4;
        else if (bomb.GetPortPlates().Any(p => p.Length == 0))
            return 5;
        else
            return 6;
    }
}
