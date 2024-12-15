using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Specialized;
using System.Net.Security;
using System.Numerics;
using System.Net.Http.Headers;
using System.ComponentModel.Design;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static UnityEditor.Experimental.GraphView.GraphView;
using TMPro;
using System.Timers;
using Unity.VisualScripting;
using System.ComponentModel;
using UnityEngine.UIElements;
using UnityEngine.Windows;

/* totalCardValue: (initialized in Card class)
To compare the value of each card I combined the
value of the most important value (1-10, Jack(11) ..etc) 
and the suits that take precedence, which is Diamonds is 4, Hearts is 3, etc.
Its ordered in this way -> (value)(suit); 6 of diamonds is "64" or (6)(4)

For example: 
Together that makes the lowest possible TCV (totalcardValue): 21 or 2 of clubs because its value is 2 and its suit clubs
is the lowest of the 4, making it one

The highest possible value is 144 or Ace of Diamonds, since an Ace is 14 and its the highest out of the 4

*/


class GameController : MonoBehaviour
{
    public static UnityEngine.Vector3 deckPosition = new UnityEngine.Vector3(0, 2, 0);
    public int round;
    public int totalRounds;
    float speed = .5f;
    public static GameController Instance { get; private set; }
    public Transform DeckParent;
    public Transform GamesTable;
    public Transform Selection1;
    public Transform Selection2;
    public Light hoverLight;
    public TMP_Text Text1;
    public TMP_Text Text2;
    public TMP_Text Mainpot;
    public TMP_Text Player1Score;
    public TMP_Text Player2Score;
    public TMP_Text Player1Chips;
    public TMP_Text Player2Chips;
    public TMP_Text Player1ChipsBet;
    public TMP_Text Player2ChipsBet;
    public TMP_Text Player1GameChips;
    public TMP_Text Player2GameChips;
    public TMP_Text Player1Name;
    public TMP_Text Player2Name;
    public TMP_Text roundText;
    public TMP_InputField Player1inputBet;
    public TMP_Text inputBetTextBelow;
    private Deck deck;
    private List<Player> players;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("More than one GameController in the scene!");
            Destroy(gameObject);
        }
    }
    void Start()
    {
        Player1inputBet.Select();
        Player1inputBet.ActivateInputField();
        Player1inputBet.gameObject.SetActive(false);
    
        Player player1 = new Player(false, 1, Selection1, Player1ChipsBet, Player1Chips, Player1inputBet); // User
        Player player2 = new Player(true, 2, Selection1, Player2ChipsBet, Player2Chips, Player1inputBet); // CPU
        players = new List<Player> { player1, player2 };
        Player1Name.text = player1.name;
        Player2Name.text = player2.name;

        List<Transform> Selections = new List<Transform> { Selection1, Selection2 };

        deck = new Deck(1, DeckParent, GamesTable, true);
        totalRounds = deck.GetBackendDeck().Count() / (players.Count() * 3);

        CardSelect.OnCardClicked += PickCard;
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        int sidepot = 0;
        int mainpot = 0;
        int round = 1;
        int deal = 0;
        int startingBet = 0;
        int lowestBet = 0;
        Boolean settledBet = false;
        Boolean initialBet = false;
        Dictionary<Player, Card> roundCards = new Dictionary<Player, Card>();
        Dictionary<Player, int> bets = new Dictionary<Player, int>();
        Dictionary<Player, int>roundBets = new Dictionary<Player, int>();
        foreach (Player player in players)
        {
            roundCards.Add(player, null);
            bets.Add(player, 0);
        }

        
        while ((deck.GetBackendDeck().Count - deal) >= 6)
        {
            Debug.Log(deck.GetBackendDeck().Count - deal);
            settledBet = false;
            roundBets.Clear();
            Print(Player1Name, $"{players[0].name}");
            Print(Player2Name, $"{players[1].name}");
            Print(Player1ChipsBet, $"{players[0].getBet().ToString()} chips");
            Print(Player2ChipsBet, $"{players[1].getBet().ToString()} chips");
            Print(Player1GameChips, $"{players[0].getGameChips().ToString()} chips");
            Print(Player2GameChips, $"{players[1].getGameChips().ToString()} chips");
            Print(Player1Chips, $"{players[0].getChips().ToString()} chips");
            Print(Player2Chips, $"{players[1].getChips().ToString()} chips");
            int iteration = 0;

            if (round != 1)
            {

                IEnumerable<Player> outOfChipsPlayers = players.Where(p => p.getGameChips() == 0);
                foreach (Player player in outOfChipsPlayers)
                {
                    int minimum = startingBet / 2;
                    int maximum = players.OrderByDescending(p => p.getGameChips()).FirstOrDefault().getGameChips();
                    Print(Text1, $"{player.name} has to add chips or fold.");
                    yield return new WaitForSeconds(speed * 1);
                    Visibility(Text2, true);
                    Print(Text2, $"Min: {minimum} Max: {maximum}");
                    yield return StartCoroutine(player.AddtoChips(inputBetTextBelow, minimum, maximum));
                    Visibility(Text2, false);
                    Player1GameChips.text = $"{players[0].getGameChips().ToString()} chips";
                    Player2GameChips.text = $"{players[1].getGameChips().ToString()} chips";
                    Print(Text1, $"{player.name} added {player.getGameChips()}.");
                    yield return new WaitForSeconds(speed * 2);
                }
            }
            



            foreach (Player player in players)
            {
                if (bets.Count == 1) { break; }
                player.validBet = false;
                if (player.fold == false)
                {
                    if (player.initialBet) Print(Text1, $"Waiting for {player.name} to put a starting bet.");
                    else Print(Text1, $"Waiting for {player.name} to place a bet.");
                    if (round == 1) yield return StartCoroutine(player.Bet(inputBetTextBelow, lowestBet, round));
                    else yield return StartCoroutine(player.Bet(inputBetTextBelow, roundBets.OrderByDescending(b => b.Value).FirstOrDefault().Value, round));
                    yield return new WaitForSeconds(Instance.speed * 2);
                    Debug.Log($"round bets: {roundBets.Count()}");
                    if (bets.Count == 1) { break; } 
                    else if (player.fold == true)
                    {
                        bets.Remove(player);
                        roundBets.Remove(player);
                        roundCards.Remove(player);
                        Print(Text1, $"{player.name} folds.");
                        Print(Player1GameChips, $"{players[0].getGameChips().ToString()} chips");
                        Print(Player2GameChips, $"{players[1].getGameChips().ToString()} chips");
                        yield return new WaitForSeconds(Instance.speed * 2);
                        continue;
                    }
                    else if (player.initialBet)
                    {
                        Print(Text1, $"{player.name} puts in {player.getStartingBet()} chips.");
                        yield return new WaitForSeconds(Instance.speed * 2);
                        player.initialBet = false;
                        Print(Player1GameChips, $"{players[0].getGameChips().ToString()} chips");
                        Print(Player2GameChips, $"{players[1].getGameChips().ToString()} chips");
                        Print(Player1ChipsBet, $"{players[0].getStartingBet().ToString()} chips");
                        Print(Player2ChipsBet, $"{players[1].getStartingBet().ToString()} chips");
                        roundBets[player] = player.getStartingBet();
                        yield return new WaitForSeconds(Instance.speed * 2);
                        continue;
                    }
                    else if (player.getGameChips() == 0) Print(Text1, $"{player.name} goes all in.");
                    else if (iteration == 0) Print(Text1, $"{player.name} put in {player.getBet()} chips.");
                    else if (player.getBet() > roundBets.OrderByDescending(b => b.Value).FirstOrDefault().Value) Print(Text1, $"{player.name} raised to {player.getBet()} chips.");
                    else if (player.getBet() == roundBets.OrderByDescending(b => b.Value).FirstOrDefault().Value) Print(Text1, $"{player.name} matched {player.getBet()} chips.");
                    Print(Player1ChipsBet, $"{players[0].getBet().ToString()} chips");
                    Print(Player2ChipsBet, $"{players[1].getBet().ToString()} chips");
                    Print(Player1GameChips, $"{players[0].getGameChips().ToString()} chips");
                    Print(Player2GameChips, $"{players[1].getGameChips().ToString()} chips");
                    roundBets[player] = player.getBet();
                    iteration++;
                    yield return new WaitForSeconds(Instance.speed * 2);
                    yield return null;
                }
                    
            }
            while (!settledBet)
            {
                if (bets.Count == 1) { break; };
                int highestBet = roundBets.OrderByDescending(v => v.Value).First().Value;
                int validBets = 0;
                foreach (Player player in roundBets.Keys)
                {
                    if (player.allIn == true) validBets++;
                    else if (player.getBet() == highestBet) validBets++;
                    yield return null;
                }

                if (!initialBet) 
                {
                    startingBet = roundBets.OrderByDescending(v => v.Value).Last().Value;
                    Print(Text1, $"The starting bet is {startingBet} chips.");
                    yield return new WaitForSeconds(Instance.speed * 2);
                    foreach ( Player player in players)
                    {
                        roundBets[player] = startingBet;
                        player.setGameChips(startingBet);
                        player.resetBet();
                    }
                    Print(Player1GameChips, $"{players[0].getGameChips().ToString()} chips");
                    Print(Player2GameChips, $"{players[1].getGameChips().ToString()} chips");

                    initialBet = true;
                    settledBet = true;
                }
                else if (validBets == roundBets.Count())
                {
                    foreach (var bet in roundBets)
                    {
                        int split1 = ((bet.Value * 70) / 100);
                        int split2 = bet.Value - split1;

                        Debug.Log("Split1: " + split1);
                        Debug.Log("Split2: " + split2);

                        sidepot += split1;
                        mainpot += split2;

                        bet.Key.changeGameChips(-bet.Value);
                        bet.Key.validBet = false;
                        bet.Key.resetBet();
                    }
                    Mainpot.text = "Mainpot: " + mainpot.ToString() + " Sidepot: " + sidepot.ToString();
                    Player1ChipsBet.text = "";
                    Player2ChipsBet.text = ""; 
                    Player1GameChips.text = players[0].getGameChips().ToString() + " chips";
                    Player2GameChips.text = players[1].getGameChips().ToString() + " chips";
                    settledBet = true;
                    
                }
                else
                {
                    highestBet = roundBets.OrderByDescending(v => v.Value).First().Value;
                    foreach (var bet in roundBets.OrderByDescending(v => v.Value))
                    {
                        Player player = bet.Key;
                        if (bet.Value < roundBets.OrderByDescending(v => v.Value).First().Value)
                        {
                            Print(Text1, $"Waiting for {player.name} to match or raise {highestBet}.");
                            yield return StartCoroutine(player.Bet(inputBetTextBelow, roundBets.OrderByDescending(b => b.Value).First().Value, round));
                            yield return new WaitForSeconds(Instance.speed * 2);
                            Debug.Log("Matched");
                            Debug.Log(player.getBet() == roundBets.OrderByDescending(b => b.Value).First().Value);
                            if (player.getBet() > roundBets.OrderByDescending(b => b.Value).First().Value) Print(Text1, $"{player.name} raised to {player.getBet()} chips.");
                            else if (player.getBet() == roundBets.OrderByDescending(b => b.Value).First().Value) Print(Text1, $"{player.name} matched {player.getBet()} chips.");
                            else if (player.fold) { roundBets.Remove(player); bets.Remove(player); }
                            Print(Player1ChipsBet, $"{players[0].getBet().ToString()} chips");
                            Print(Player2ChipsBet, $"{players[1].getBet().ToString()} chips");
                            Print(Player1GameChips, $"{players[0].getGameChips().ToString()} chips");
                            Print(Player2GameChips, $"{players[1].getGameChips().ToString()} chips");
                            roundBets[player] = player.getBet();
                            yield return new WaitForSeconds(Instance.speed * 2);
                        }
                    }
                }
            }
            if (bets.Count == 1) { break; };
            roundText.text = $"Round {round} of {totalRounds}";
            Player1ChipsBet.text = "";
            Player2ChipsBet.text = "";
            foreach (Player player in players)
            {
                Print(Player1Score, players[0].points.ToString(), "Player 1: ");
                Print(Player2Score, players[1].points.ToString(), "Player 2: ");


                Print(Text1, $"{player.name}'s turn.");
                yield return new WaitForSeconds(Instance.speed * 3);
                CardSelection cards = new CardSelection(deck, roundCards, player, deal);
                yield return StartCoroutine(cards.PromptPick(Text1, Text2)); // Wait for user to pick a card
                

                Print(Player1Score, players[0].points.ToString(), "Player 1: ");
                Print(Player2Score, players[1].points.ToString(), "Player 2: ");
                yield return new WaitForSeconds(Instance.speed * 1);

                deal += 3;
            }
            Player roundWinner = roundCards.OrderByDescending(roundCard => roundCard.Value.getTCV()).First().Key;
            roundWinner.changeGameChips(sidepot);
            Print(Text1, $"{roundWinner.name} won the sidebet.");
            Visibility(Text2, true);
            Print(Text2, $"+{sidepot} chips.");
            sidepot = 0;
            Mainpot.text = "Mainpot: " + mainpot.ToString() + " Sidepot: " + sidepot.ToString();
            yield return new WaitForSeconds(Instance.speed * 3);
            Visibility(Text2, false);
            round++;
            Debug.Log("Round " + round.ToString());
        }
        Print(Player1Score, players[0].points.ToString(), "Player 1: ");
        Print(Player2Score, players[1].points.ToString(), "Player 2: ");
        if ((deck.GetBackendDeck().Count - deal) <= 3) {
            Debug.Log("Hello");
            Player winner = roundBets.OrderByDescending(roundCard => roundCard.Key.points).First().Key;
            Print(Text1, "You ran out of cards!");
            yield return new WaitForSeconds(Instance.speed * 2);
            List<int> points = new List<int>();
            players = players.OrderByDescending(player => player.points).ToList(); // Orders players from least to greatest based on points
            Print(Text1, $"{winner.name} won with {winner.points} points!");
            winner.changeGameChips(mainpot);
            Player1GameChips.text = players[0].getGameChips().ToString() + " chips";
            Player2GameChips.text = players[1].getGameChips().ToString() + " chips";
            winner.addChips(winner.getGameChips());
            mainpot = 0;
            Mainpot.text = "Mainpot: " + mainpot.ToString() + " Sidepot: " + sidepot.ToString();
            Print(Player1Chips, $"{players[0].getChips().ToString()} chips");
            Print(Player2Chips, $"{players[1].getChips().ToString()} chips");

        }
        else if (bets.Count == 1)
        {
            Player winner = roundBets.Keys.First();
            int total = 0;
            foreach (Player player in players) { winner.addChips(player.getGameChips()); total += player.getGameChips(); }
            winner.changeGameChips(mainpot);
            winner.changeGameChips(sidepot);
            total += mainpot; total += sidepot;
            Print(Text1, $"{players[0].name} wins {total} chips.");
            yield return new WaitForSeconds(Instance.speed * 2);
            winner.addChips(winner.getGameChips());
        }
        else
        {
            foreach (Player player in players)
            {
                player.changeGameChips(startingBet);
            }
        }

        Print(Player1Chips, $"{players[0].getChips().ToString()} chips");
        Print(Player2Chips, $"{players[1].getChips().ToString()} chips");


        foreach (Player player in players) { player.Reset(); } //Resets points after game is finished    
    }
    

    private void PickCard(int cardIndex)
    {
        if (deck.GetBackendDeck()[cardIndex].flip == true)
        {
            FlipCard(cardIndex);
            deck.GetBackendDeck()[cardIndex].chosen = true;
        }
    }

    public void FlipCard(int cardIndex)
    {
        GameObject cardPicked = deck.GetVisualDeck()[cardIndex];
        float px = cardPicked.transform.localPosition.x;
        float py = cardPicked.transform.localPosition.y;
        float pz = cardPicked.transform.localPosition.z;

        float rx = cardPicked.transform.localRotation.x;

        if (rx < 0)
        {
            cardPicked.transform.localRotation = UnityEngine.Quaternion.Euler(90, 0, 0);
            cardPicked.transform.localPosition = new UnityEngine.Vector3(px, py + .5f, pz);
        }
        else
        {
            cardPicked.transform.localRotation = UnityEngine.Quaternion.Euler(-90, 0, 0);
            cardPicked.transform.localPosition = new UnityEngine.Vector3(px, py + .5f, pz);
        }
    }
    void Visibility(TMP_Text obj, bool visibility)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visibility;
        }
    }
    void Visibility(TMP_InputField obj, bool visibility)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visibility;
        }
    }
    void Visibility(GameObject obj, bool visibility)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visibility;
        }
    }
    void Visibility(Light obj, bool visibility)
    {
        obj.enabled = visibility;
    }

    public void Print(TMP_Text text, string message, string startingMessage = "")
    {
        StartCoroutine(PrintLoop(text, message, startingMessage));
    }
    public IEnumerator PrintLoop(TMP_Text text, string message, string startingMessage = "")
    {
        text.text = startingMessage;
        foreach (char letter in message)
        {
            text.text += letter;
            yield return new WaitForSeconds(Instance.speed * .04f);
        }
    }

    public void moveLight(int pos)
    {
        switch (pos) 
        {
            case 0:
                hoverLight.transform.localPosition = new UnityEngine.Vector3(3.79f, 2.58f, -3.57f);
                break;
            case 1:
                hoverLight.transform.localPosition = new UnityEngine.Vector3(3.295f, 2.546f, -3.581f);
                break;
            case 2:
                hoverLight.transform.localPosition = new UnityEngine.Vector3(2.796f, 2.346f, -3.635f);
                break;
        }
    }

    public Light getCardLight()
    {
        return hoverLight;
    }


    class Player
    {
        public int points;
        public int id;
        private int totalChipsBet;
        private int startingBet;
        private int gameChips;
        private int bet;
        private int chips;
        public string name;
        public bool cpu;
        public bool fold = false;
        public bool allIn;
        public bool validBet = false;
        public bool initialBet = true;
        
        public Transform Selection;
        public TMP_Text chipsTextBox;
        public TMP_Text chipsBetTextBox;
        public TMP_InputField betInput;

        public Player(bool cpu, int id, Transform Selection, TMP_Text chipsBetTextBox, TMP_Text chipsTextBox, TMP_InputField betInput)
        {
            chips = 100;
            this.name = "Player" + id;
            this.id = id;
            this.cpu = cpu;
            this.chipsBetTextBox = chipsBetTextBox;
            this.chipsTextBox = chipsTextBox;
            this.Selection = Selection;
            this.betInput = betInput;
        }
        public IEnumerator Bet(TMP_Text betText, int significantBet, int round) {

            // Enable input
            validBet = false;
            while (!validBet)
            {
                betInput.gameObject.SetActive(true);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("Pressed Enter");
                    if (betInput.text == "FOLD") {
                        fold = true;
                        validBet = true;
                    }
                    else if (int.TryParse(betInput.text, out int bet))
                    {

                        Debug.Log("Point");
                        Debug.Log($"Bet: {bet}");
                        Debug.Log($"Highest: {significantBet}");
                        Debug.Log($"Total Chips Bet: {totalChipsBet}");
                        Debug.Log($"Game Chips: {gameChips}");
                        if (round == 1)
                        {
                            if (significantBet != 0)
                            {
                                if (bet > significantBet) betText.text = "You can't bet more than the lowest.";
                                else if (bet > chips) betText.text = "Insufficient Chips";
                                else { startingBet = bet; validBet = true; }

                            }
                            else
                            {
                                if (bet < 20)
                                {
                                    betText.text = "You have to bet more than 20.";
                                }
                                else if (bet > chips) betText.text = "Insufficient Chips";
                                else { startingBet = bet; validBet = true; }
                            } 
                        }
                        else
                        {
                            if (significantBet != 0)
                            {
                                if (significantBet > bet)
                                {
                                    if (bet == gameChips)
                                    { 
                                        totalChipsBet = bet;
                                        allIn = true;
                                        validBet = true;
                                       
                                    }
                                    else if ((bet + totalChipsBet) < significantBet) betText.text = "All in or fold.";
                                }
                                else if ((bet + totalChipsBet) < significantBet) betText.text = "You have to match or raise";
                                else
                                {
                                    totalChipsBet = bet;
                                    this.validBet = true;
                                    validBet = true;
                                }
                            }
                            else if (bet > gameChips) betText.text = "Insufficient Chips";
                            else
                            {
                                totalChipsBet = bet;
                                validBet = true;
                            }
                        }
                    }
                    else { betInput.text = "Incorrect Characters"; }
                }
                yield return null;
            
            }
            betInput.gameObject.SetActive(false);
            betInput.text = "";
            betText.text = "";
            yield return null;
        }

        public IEnumerator AddtoChips(TMP_Text betText, int minimum, int maximum)
        {

            validBet = false;
            while (validBet == false)
            {
                betInput.gameObject.SetActive(true);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("Pressed Enter");
                    if (betInput.text == "FOLD")
                    {
                        fold = true;
                        validBet = true;
                    }
                    else if (int.TryParse(betInput.text, out int add))
                    {
                        if (add < minimum) { betText.text = $"Minimum: {minimum}"; }
                        else if (add > maximum) { betText.text = $"Maximum: {maximum}"; }
                        else
                        {
                            gameChips += add;
                            validBet = true;
                        }
                    }
                }
                yield return null;
            }
            betInput.text = "";
            betText.text = "";
        }

        public int getChips() { return chips; }
        public void addChips(int change) { chips += change; }
        public void setChips(int change) { chips = change; }
        public int getBet() { return totalChipsBet; }
        public int getStartingBet() { return  startingBet; }
        public void resetBet() { totalChipsBet = 0; }

        
        public int getGameChips() { return gameChips; }
        public void setGameChips(int change) { gameChips = change; }
        public void changeGameChips(int change) { gameChips += change; }
        public void Reset() { points = 0; }
    }
    class Card
    {
        public int totalCardValue; // See above for definition
        private int index;
        public int value;
        public int suitLevel;
        public string suit;
        public string name;
        public string rank;
        public bool flip = false;
        public bool chosen = false;

        public Card()
        {
            suit = string.Empty;
            value = 0;
            
        }
        public Card(string card)
        {
            assignValues(card);
        }

        public void assignValues(string card)
        {
            var cardValues = card.Split(new string[] { " of " }, StringSplitOptions.None); // Splits  these into seperate variables
            string Value = cardValues[0].Trim();
            if (Value == "Jack") this.value = 11;

            else if (Value == "Queen") this.value = 12;

            else if (Value == "King") this.value = 13;

            else if (Value == "Ace") this.value = 14;

            else this.value = int.Parse(Value);

            this.rank = Value;

            this.suit = cardValues[1].Trim();

            this.suitLevel = Array.IndexOf(Deck.suits, this.suit) + 1;

            this.totalCardValue = value * 10 + suitLevel;

            this.name = $"{card}s";
        }

        public int getIndex()
        {
            return index;
        }
        public void setIndex(int index)
        {
            this.index = index;
        }
        public int getTCV()
        {
            return totalCardValue;
        }

    }

    class Deck
    {
        private List<GameObject> visualDeck = new List<GameObject>();
        private List<Card> backendDeck = new List<Card>();
        public static string[] suits = { "Club", "Spade", "Heart", "Diamond" };
        public static string[] values = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
        public Transform DeckParent;
        private Transform GamesTable;
        public TMP_Text messageText;

        public Deck(int copies, Transform DeckParent, Transform GamesTable, bool shuffle)
        {
            this.GamesTable = GamesTable;
            this.DeckParent = DeckParent;
            for (int i = 0; i < copies; i++)
            {
                foreach (string value in values)
                {
                    foreach (string suit in suits) // Fills the deck using the arrays above
                    {
                        Card card1 = new Card($"{value} of {suit}");
                        
                        backendDeck.Add(card1);
                    }
                }
            } 
            if (shuffle) Shuffle(3);
            for (int i = 0; i < backendDeck.Count; i++) {
                backendDeck[i].setIndex(i);
            }
            Instance.StartCoroutine(SpawnCard(DeckParent));
        }

        public IEnumerator SpawnCard(Transform DeckParent)
        {
            List<Card> localDeck = new List<Card>(backendDeck);
            foreach (Card card in localDeck)
                {
                    string cardName = $"Card_{card.suit}{card.rank}";
                    GameObject cardPrefab = Resources.Load<GameObject>($"Prefabs/Individual Pieces/Cards/{card.suit}s/{cardName}");
                    if (cardPrefab != null)
                    {
                        
                  
                        GameObject visualCard = Instantiate(cardPrefab, deckPosition, UnityEngine.Quaternion.Euler(90, 0, 0), DeckParent);
                        visualCard.transform.localScale = new UnityEngine.Vector3(2, 2, 1);
                        CardSelect cardSelect = visualCard.AddComponent<CardSelect>();
                        Rigidbody rb = visualCard.GetComponent<Rigidbody>();

                        if (rb != null)
                        {
                            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        }

                        visualCard.name = cardName;
                        visualCard.transform.localPosition = deckPosition; 

                        BoxCollider boxCollider = visualCard.GetComponent<BoxCollider>();
                        if (boxCollider != null)
                        {
                            UnityEngine.Vector3 newSize = boxCollider.size;
                            newSize.z = 0.002549444f;
                            boxCollider.size = newSize;
                        }
                        else
                        {
                            messageText.text = $"{visualCard.name} is missing a box collider";
                        }
                        cardSelect.cardIndex = visualDeck.Count;
                        visualDeck.Add(visualCard);
                        
                    yield return new WaitForSeconds(Instance.speed * 0.1f);
                    }
                    else
                    {
                        Debug.LogError($"Card prefab {cardName} not found!"); 
                    }
                }
        }
    public List<Card> GetBackendDeck() => backendDeck;
    public List<GameObject> GetVisualDeck() => visualDeck;

        public void Shuffle(int amount)
        {
            System.Random random = new System.Random();
            for (int count = 0; count < amount; count++)
            {
                for (int i = backendDeck.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    Card temp = backendDeck[i];
                    backendDeck[i] = backendDeck[j];
                    backendDeck[j] = temp;
                }
            }
        }
    }
    

    class CardSelection
    {
        public bool cardisChosen = false;
        private Card chosenCard;
        private List<Card> selectionOfCards;
        private List<Card> backendDeck;
        private List<GameObject> visualDeck;
        private Dictionary<Player, Card> roundCards;
        private static float offset = 0.5f;
        private Player player;
        private int deal;

        public CardSelection(Deck deckObj, Dictionary<Player, Card> roundCards, Player player, int deal)
        {
            this.deal = deal;
            this.backendDeck = deckObj.GetBackendDeck();
            this.visualDeck = deckObj.GetVisualDeck();
            this.roundCards = roundCards;
            this.player = player;
            this.selectionOfCards = new List<Card>();

            for (int i = deal; i < (deal + 3); i++)
            {
                
                int iteration = i - deal;
                GameObject gameObject = visualDeck[i];
                gameObject.transform.SetParent(player.Selection.transform);
                gameObject.transform.localPosition = UnityEngine.Vector3.zero;
                selectionOfCards.Add(backendDeck[i]);
                backendDeck[i].flip = true;


                switch (iteration) {
                    case 0:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(offset, 0, 0);
                        break;
                    case 1:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(0, 0, 0);
                        break;
                    case 2:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(-offset, 0, 0);
                        break;
                }
                

            }
            

        }
        public IEnumerator PromptPick(TMP_Text Text1, TMP_Text Text2)
        {
            System.Random random = new System.Random();
            int choice = player.cpu ? random.Next(0, 3) : 0;
            Card hoveredCard = null;
            cardisChosen = false;
            if (!player.cpu)
            {
                while (!cardisChosen)
                {
                    Instance.Visibility(Instance.getCardLight(), true);
                    hoveredCard = DetectHover();    
                    if (hoveredCard != null)
                    {
                        Instance.moveLight(selectionOfCards.IndexOf(hoveredCard));
                        if (hoveredCard.chosen != false)
                        {
                            Instance.moveLight(selectionOfCards.IndexOf(hoveredCard));
                            chosenCard = hoveredCard;
                            cardisChosen = true;
                        }
                    }
                    yield return null;

                }
                foreach (Card card in selectionOfCards) card.flip = false;
                
                Instance.Print(Text1, $"You choose {chosenCard.name}.");
            }
            else
            {
                foreach (Card card in selectionOfCards) card.flip = false;
                
                Instance.Print(Text1, $"{player.name} is choosing...");
                yield return new WaitForSeconds(Instance.speed * 2);

                chosenCard = selectionOfCards[choice];
                Instance.moveLight(selectionOfCards.IndexOf(chosenCard));
                Instance.Visibility(Instance.getCardLight(), true);
                backendDeck[choice + deal].chosen = true;
                Instance.FlipCard(choice + deal);

            }
            yield return new WaitForSeconds(Instance.speed * 2.5f);
            Instance.Print(Text1, "Revealing Cards..");
            yield return new WaitForSeconds(Instance.speed * 2);

            foreach (Card card in selectionOfCards)
            {
                if (card.chosen == false) Instance.FlipCard(card.getIndex());
            }
            chosenCard.chosen = false;
            yield return new WaitForSeconds(Instance.speed * 1);
            
            int value = selectionOfCards.OrderByDescending(c => c.totalCardValue).ToList().IndexOf(chosenCard);
            switch (value)
            {
                case 0:
                    player.points += 10;
                    Instance.Print(Text1, $"{player.name} pulled the highest!");
                    yield return new WaitForSeconds(Instance.speed * 1.5f);
                    Instance.Print(Text2, "+10 points");
                    break;
                case 1:
                    player.points += 3;
                    Instance.Print(Text1, $"{player.name} pulled the middle!");
                    yield return new WaitForSeconds(Instance.speed * 1.5f);
                    Instance.Print(Text2, "+3 points");
                    break;
                case 2:
                    player.points -= 3;
                    Instance.Print(Text1, $"{player.name} pulled the lowest..");
                    yield return new WaitForSeconds(Instance.speed * 1.5f);
                    Instance.Print(Text2, "-3 points");
                    break;
            }
            yield return new WaitForSeconds(Instance.speed * 1.5f);

            roundCards[player] = chosenCard;
            Instance.Visibility(Text2, false);
            Instance.Visibility(Instance.getCardLight(), false);
            Instance.Print(Text1, $"{player.name} now has {player.points} points!");
            yield return new WaitForSeconds(Instance.speed * 5);

            for (int i = deal; i < (deal+3); i++)
            {
                visualDeck[i].transform.localPosition = new UnityEngine.Vector3(20, 20, 20);
                cardisChosen = false;
            }

        }
        private Card DetectHover()
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                for (int i = 0; i < selectionOfCards.Count; i++)
                {
                    
                    if (hit.collider.gameObject == visualDeck[i+deal])
                    {
                        return selectionOfCards[i];
                    }
                }
            }

            return null; 
        }
    }
}

    