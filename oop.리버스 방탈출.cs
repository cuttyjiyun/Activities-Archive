using System;
using System.Collections.Generic;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ReverseEscapeRoom
{
    public enum PlayerState { Alive, Dead, Trapped, Escaped }
    public enum TrapState { Active, Triggered, Disabled }
    public enum ItemType { Hint, Hammer, HealItem }
    public enum EventType { FindItem, UseItem, OpenDoor, Trap, SolvePuzzle, Fail, Clear, Heal }
    public enum Rank { S, A, B, C, D }
    public enum RoomType { StartRoom, TrapRoom, SafeRoom, DoorRoom, HintRoom, ItemRoom }

    //  1. Inventory
    public class Inventory
    {
        private readonly List<Item> _items = new List<Item>();

        public IReadOnlyList<Item> Items => _items.AsReadOnly();
        public int Count => _items.Count;

        public void AddItem(Item item)
        {
            _items.Add(item);
            Console.WriteLine($"[인벤토리] '{item.Name}' 추가됨.");
        }

        public bool RemoveItem(Item item)
        {
            bool removed = _items.Remove(item);
            if (removed) Console.WriteLine($"[인벤토리] '{item.Name}' 제거됨.");
            return removed;
        }

        public bool HasItem(ItemType type) =>
            _items.Exists(i => i.Type == type && i.IsCollected);

        public Item GetItem(ItemType type) =>
            _items.Find(i => i.Type == type && i.IsCollected);

        public void PrintInventory()
        {
            if (_items.Count == 0)
            {
                Console.WriteLine("[인벤토리] 비어 있음.");
                return;
            }
            Console.WriteLine("[인벤토리] 보유 아이템:");
            foreach (var item in _items)
                Console.WriteLine($"  - {item.Name} : {item.Description}");
        }
    }

    //  2. Item
    public class Item
    {
        public string Name { get; private set; }
        public ItemType Type { get; private set; }
        public string Description { get; private set; }
        public string HintMessage { get; set; }
        public int HealAmount { get; private set; }
        public bool IsCollected { get; private set; }

        public Item(string name, ItemType type, string description,
                    string hintMessage = "", int healAmount = 0)
        {
            Name = name;
            Type = type;
            Description = description;
            HintMessage = hintMessage;
            HealAmount = healAmount;
            IsCollected = false;
        }

        public void Collect() => IsCollected = true;

        public void PrintItemInfo()
        {
            Console.WriteLine("[아이템 발견]");
            Console.WriteLine($"  이름 : {Name}");
            Console.WriteLine($"  설명 : {Description}");
        }

        public void PrintHint()
        {
            Console.WriteLine();
            Console.WriteLine("===== 힌트 =====");
            Console.WriteLine(HintMessage);
            Console.WriteLine("================");
        }
    }

    //  3. Tool
    public class Tool
    {
        private string _answerPassword = "";
        private string _autoPassword = "";

        public void SetAnswerPassword(string password) => _answerPassword = password;

        public void UseHint(Item hintItem)
        {
            Console.WriteLine("\n[Tool] 힌트 사용");
            if (hintItem == null || !hintItem.IsCollected)
            {
                Console.WriteLine("힌트 아이템을 획득하지 못했습니다.");
                return;
            }
            hintItem.PrintHint();
        }

        public void GuessPasswordByHint(Item hintItem)
        {
            if (hintItem == null || !hintItem.IsCollected)
            {
                Console.WriteLine("힌트를 얻지 못해 비밀번호를 유추할 수 없습니다.");
                return;
            }
            _autoPassword = _answerPassword;
            Console.WriteLine("\n[자동 추론] 플레이어가 힌트를 바탕으로 비밀번호를 유추했습니다.");
        }

        public bool CheckPasswordAuto()
        {
            Console.WriteLine("\n[자동 비밀번호 입력]");
            if (string.IsNullOrEmpty(_autoPassword))
            {
                Console.WriteLine("아직 유추한 비밀번호가 없습니다.");
                return false;
            }
            Console.WriteLine($"[AI] 비밀번호 '{_autoPassword}' 자동 입력");
            bool correct = _autoPassword == _answerPassword;
            Console.WriteLine(correct ? "비밀번호 일치!" : "비밀번호 불일치!");
            return correct;
        }

        public bool CanUseHammer(Item hammerItem)
        {
            if (hammerItem == null || !hammerItem.IsCollected)
            {
                return false;
            }
            return true;
        }

        public void UseHealItem(Player player, Item healItem)
        {
            Console.WriteLine("\n[Tool] 힐템 사용");
            if (healItem == null || !healItem.IsCollected)
            {
                Console.WriteLine("힐템을 획득하지 못했습니다.");
                return;
            }
            player.Heal(healItem.HealAmount);
            Console.WriteLine($"체력 {healItem.HealAmount} 회복.");
        }
    }

    //  4. Trap
    public class Trap
    {
        public string Name { get; }
        public int Row { get; }
        public int Col { get; }
        public TrapState State { get; protected set; } = TrapState.Active;

        public Trap(string name, int row, int col)
        {
            Name = name;
            Row = row;
            Col = col;
        }

        public bool IsOnSameCell(int row, int col) =>
            row == Row && col == Col;

        public void Trigger()
        {
            if (State != TrapState.Active) return;
            State = TrapState.Triggered;
            Console.WriteLine($"[함정:{Name}] ({Row + 1},{Col + 1}) 발동!");
        }

        public void Disable()
        {
            State = TrapState.Disabled;
            Console.WriteLine($"[함정:{Name}] 비활성화됨.");
        }

        public override string ToString() =>
            $"Trap '{Name}'({Row + 1},{Col + 1}) - {State}";
    }

    public class TeleportTrap : Trap
    {
        private readonly Random _rng;
        private readonly int _mapSize;

        public TeleportTrap(int row, int col, int mapSize)
            : base("순간이동함정", row, col)
        {
            _mapSize = mapSize;
            _rng = new Random();
        }

        public void Trigger(ref int playerX, ref int playerY)
        {
            if (State != TrapState.Active) return;

            int newX, newY;
            do
            {
                newX = _rng.Next(_mapSize);
                newY = _rng.Next(_mapSize);
            }
            while (newX == playerX && newY == playerY);

            Console.WriteLine($"[함정:{Name}] 발동! ({playerX + 1},{playerY + 1}) → ({newX + 1},{newY + 1}) 순간이동!");
            playerX = newX;
            playerY = newY;
            State = TrapState.Triggered;
        }
    }

    public class DamageTrap : Trap
    {
        public int DamageAmount { get; }

        public DamageTrap(int row, int col, int damageAmount = 5)
            : base("체력감소함정", row, col)
        {
            DamageAmount = damageAmount;
        }

        public void Trigger(Player player)
        {
            if (State != TrapState.Active) return;
            Console.WriteLine($"[함정:{Name}] 발동! 체력 -{DamageAmount}");
            player.TakeDamage(DamageAmount);
            State = TrapState.Triggered;
        }
    }

    public class TimeTrap : Trap
    {
        public int TimePenalty { get; }

        public TimeTrap(int row, int col, int timePenalty = 5)
            : base("시간감소함정", row, col)
        {
            TimePenalty = timePenalty;
        }

        public void Trigger(TimeLimit timeLimit)
        {
            if (State != TrapState.Active) return;
            Console.WriteLine($"[함정:{Name}] 발동! 감소된 시간 -{TimePenalty}초");
            timeLimit.AddElapsedTime(TimePenalty);
            State = TrapState.Triggered;
        }
    }

    //  5. Door
    public class Door
    {
        public int Row { get; }
        public int Col { get; }
        public bool IsFake { get; set; }
        public bool IsLocked { get; private set; } = false;

        public Door(int row, int col, bool isFake = false)
        {
            Row = row;
            Col = col;
            IsFake = isFake;
        }

        public void Lock()
        {
            if (IsLocked) return;
            IsLocked = true;
            Console.WriteLine($"[문] ({Row + 1},{Col + 1}) 문이 잠겼습니다!");
        }

        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            Console.WriteLine($"[문] ({Row + 1},{Col + 1}) 문이 열렸습니다!");
        }

        public override string ToString() =>
            $"Door({Row + 1},{Col + 1}) [{(IsFake ? "가짜" : "진짜")}] [{(IsLocked ? "잠김" : "열림")}]";
    }

    //  6. Room
    public class Room
    {
        public RoomType Type { get; set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool IsVisited { get; set; } = false;

        public Room(RoomType type, int x, int y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public int Enter()
        {
            IsVisited = true;
            Console.WriteLine($"\n[방({X + 1},{Y + 1})] 입장!");

            switch (Type)
            {
                case RoomType.StartRoom: Console.WriteLine("시작 방입니다."); return 0;
                case RoomType.SafeRoom: return 0;
                case RoomType.HintRoom: Console.WriteLine("힌트를 발견했습니다!"); return 0;
                case RoomType.DoorRoom: Console.WriteLine("문 발견!"); return 0;
                case RoomType.TrapRoom: Console.WriteLine("함정을 밟았습니다!"); return 5;
                case RoomType.ItemRoom: Console.WriteLine("아이템 발견!"); return 0;
            }
            return 0;
        }
    }

    //  7. EventSystem
    public class Stage2ItemEventSystem
    {
        private Tool _tool;
        private Item _passwordHintItem;
        private int _hintRow;
        private int _hintCol;

        public Stage2ItemEventSystem(int hintRow, int hintCol, int doorRow, int doorCol)
        {
            _tool = new Tool();
            _hintRow = hintRow;
            _hintCol = hintCol;

            string password = $"{hintRow + 1}{hintCol + 1}{doorRow + 1}{doorCol + 1}";
            _tool.SetAnswerPassword(password);

            _passwordHintItem = new Item(
                "비밀번호 힌트", ItemType.Hint,
                "진짜 문을 열기 위한 비밀번호 힌트",
                $"힌트: 진짜 문을 열기 위한 비밀번호는 [힌트 행, 힌트 열, 진짜문 행, 진짜문 열] 번호입니다. -> 정답: {password}"
            );
        }

        public bool IsHintPosition(int row, int col) => row == _hintRow && col == _hintCol;

        public bool CheckHintEvent(Player player, int row, int col)
        {
            if (!IsHintPosition(row, col) || _passwordHintItem.IsCollected) return false;

            Console.WriteLine("\n[이벤트: Stage2 힌트 획득]");
            _passwordHintItem.PrintItemInfo();
            _passwordHintItem.Collect();
            player.Inventory.AddItem(_passwordHintItem);
            _passwordHintItem.PrintHint();
            _tool.GuessPasswordByHint(_passwordHintItem);
            return true;
        }

        public bool AutoPasswordEvent() => _tool.CheckPasswordAuto();
    }

    public class Stage3ItemEventSystem
    {
        private Tool _tool;
        private Item _hammerItem;
        private Item _healItem;
        private Item _hintItem;

        private int _hammerRow, _hammerCol;
        private int _healRow, _healCol;
        private int _hintRow, _hintCol;

        public Stage3ItemEventSystem(int hammerRow, int hammerCol, int healRow, int healCol, int hintRow, int hintCol)
        {
            _tool = new Tool();
            _hammerRow = hammerRow; _hammerCol = hammerCol;
            _healRow = healRow; _healCol = healCol;
            _hintRow = hintRow; _hintCol = hintCol;

            _hammerItem = new Item("망치", ItemType.Hammer, "함정을 없앨 수 있는 아이템");
            _healItem = new Item("힐템", ItemType.HealItem, "플레이어의 체력을 회복하는 아이템", "", 3);
            _hintItem = new Item(
                "Stage3 힌트", ItemType.Hint,
                "Stage3 진행 방법을 알려주는 힌트",
                "힌트: 망치를 먼저 획득하면 함정 칸도 지나갈 수 있고, 힐템은 체력이 부족할 때 도움이 된다."
            );
        }

        public bool CheckItemEvent(Player player, int row, int col, Room[,] map)
        {
            bool trigger1 = CheckHammerEvent(player, row, col, map);
            bool trigger2 = CheckHealEvent(player, row, col, map);
            bool trigger3 = CheckHintEvent(player, row, col);
            return trigger1 || trigger2 || trigger3;
        }

        private bool CheckHammerEvent(Player player, int row, int col, Room[,] map)
        {
            if (row != _hammerRow || col != _hammerCol || _hammerItem.IsCollected) return false;
            Console.WriteLine("\n[이벤트: 망치 획득]");
            _hammerItem.PrintItemInfo();
            _hammerItem.Collect();
            player.Inventory.AddItem(_hammerItem);

            map[row, col].Type = RoomType.SafeRoom;
            Console.WriteLine("이제 망치로 최초로 마주치는 함정을 한 번 제거할 수 있습니다.");
            Console.WriteLine("\nEnter를 누르면 계속...");
            Console.ReadLine();
            return true;
        }

        private bool CheckHealEvent(Player player, int row, int col, Room[,] map)
        {
            if (row != _healRow || col != _healCol || _healItem.IsCollected) return false;
            Console.WriteLine("\n[이벤트: 힐템 획득]");
            _healItem.PrintItemInfo();
            _healItem.Collect();
            player.Inventory.AddItem(_healItem);

            map[row, col].Type = RoomType.SafeRoom;
            _tool.UseHealItem(player, _healItem);
            return true;
        }

        private bool CheckHintEvent(Player player, int row, int col)
        {
            if (row != _hintRow || col != _hintCol || _hintItem.IsCollected) return false;
            Console.WriteLine("\n[이벤트: Stage3 힌트 획득]");
            _hintItem.PrintItemInfo();
            _hintItem.Collect();
            player.Inventory.AddItem(_hintItem);
            _hintItem.PrintHint();
            return true;
        }

        public bool TryDestroyTrap(Player player, Trap trap, Room[,] map)
        {
            Item hammer = player.Inventory.GetItem(ItemType.Hammer);
            if (!_tool.CanUseHammer(hammer)) return false;

            trap.Disable();
            player.Inventory.RemoveItem(hammer);

            map[trap.Row, trap.Col].Type = RoomType.SafeRoom;
            Console.WriteLine($"\n[망치 사용] 망치로 '{trap.Name}'을 부쉈습니다! 맵에서 [X]가 제거됩니다.");
            return true;
        }
    }

    //  8. TimeLimit
    public class TimeLimit
    {
        public int TimeLimitSeconds { get; private set; }
        public int ElapsedSeconds { get; private set; }
        public bool IsTimeUp => ElapsedSeconds >= TimeLimitSeconds;

        private readonly Timer _timer;
        private readonly Player _player;

        public event EventHandler OnTimeUp;

        public TimeLimit(Player player, int timeLimitSeconds)
        {
            _player = player;
            TimeLimitSeconds = timeLimitSeconds;
            ElapsedSeconds = 0;

            _timer = new Timer(1000);
            _timer.Elapsed += Tick;
            _timer.AutoReset = true;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
        public void AddElapsedTime(int seconds) => ElapsedSeconds += seconds;

        private void Tick(object sender, ElapsedEventArgs e)
        {
            ElapsedSeconds++;
            if (IsTimeUp)
            {
                Stop();
                Console.WriteLine("\n[규칙] 시간 초과! 탈출 실패.");
                OnTimeUp?.Invoke(this, EventArgs.Empty);
                _player.TakeDamage(_player.Health);
            }
        }
    }

    //  9. Player
    public class Player
    {
        public string Name { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public PlayerState State { get; private set; }
        public Inventory Inventory { get; private set; }
        public int TrapCount { get; private set; }

        private int _playerX = 0;
        private int _playerY = 0;
        private Random _random = new Random();

        public Player(string name, int health)
        {
            Name = name;
            Health = health;
            MaxHealth = health;
            State = PlayerState.Alive;
            Inventory = new Inventory();
        }

        public void TakeDamage(int amount)
        {
            Health = Math.Max(0, Health - amount);
            Console.WriteLine($"[{Name}] {amount} 피해! HP: {Health}/{MaxHealth}");
            if (Health == 0) State = PlayerState.Dead;
        }

        public void Heal(int amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
            Console.WriteLine($"[{Name}] {amount} 회복! HP: {Health}/{MaxHealth}");
        }

        public void SetEscaped() => State = PlayerState.Escaped;

        private void PauseForEvent()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n▶ [이벤트 발생!] 내용을 읽은 후 '아무 키'나 누르면 시뮬레이션이 계속됩니다.");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        private (int r, int c) InputPosition(string name, int mapSize, Room[,] map)
        {
            while (true)
            {
                Console.Write($"[{name}]의 배치 위치를 입력하세요 (행 열 순서로 공백 구분, 예: 1 3): ");
                string input = Console.ReadLine();
                string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2 && int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c))
                {
                    r -= 1;
                    c -= 1;

                    if (r >= 0 && r < mapSize && c >= 0 && c < mapSize)
                    {
                        if (map[r, c].Type == RoomType.SafeRoom)
                        {
                            return (r, c);
                        }
                        else
                        {
                            Console.WriteLine($"오류: 해당 위치({r + 1},{c + 1})에는 이미 다른 오브젝트({map[r, c].Type})가 존재합니다.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"오류: 맵 범위를 벗어났습니다. (1 ~ {mapSize} 사이의 숫자를 입력하세요)");
                    }
                }
                else
                {
                    Console.WriteLine("오류: 올바른 형식이 아닙니다. '행'과 '열'을 한 칸 띄우고 숫자만 정확히 입력하세요.");
                }
            }
        }

        // Stage 1
        public void StartStage1()
        {
            const int size = 3;
            Console.WriteLine($"\n=================================");
            Console.WriteLine($"   Stage 1 설정 (맵 크기: {size}x{size})");
            Console.WriteLine($"=================================");

            var map = new Room[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    map[y, x] = new Room(RoomType.SafeRoom, x, y);

            map[0, 0].Type = RoomType.StartRoom;
            map[2, 2].Type = RoomType.DoorRoom;
            Door door = new Door(2, 2, isFake: false);

            _playerX = 0; _playerY = 0;
            Console.WriteLine("\n[현재 고정 배치 상태 미니맵]");
            PrintMap(map, size);

            var (trapR, trapC) = InputPosition("함정(X)", size, map);
            map[trapR, trapC].Type = RoomType.TrapRoom;
            Trap trap = new Trap("문잠금함정", trapR, trapC);

            Console.WriteLine("\n=== Stage 1 시뮬레이션 시작 ===");
            PrintMap(map, size);
            map[0, 0].Enter();

            while (State == PlayerState.Alive)
            {
                System.Threading.Thread.Sleep(1000);
                Console.Clear();

                AutoMove(map, size);
                Console.WriteLine($"\n플레이어 이동 → ({_playerX + 1},{_playerY + 1})");

                Room cur = map[_playerY, _playerX];
                cur.Enter();

                if (trap.IsOnSameCell(_playerY, _playerX) && trap.State == TrapState.Active)
                {
                    trap.Trigger();
                    door.Lock();
                    TrapCount++;
                    Console.WriteLine(">>> 함정 발동! 문이 잠겼습니다. 탈출 실패.");
                    State = PlayerState.Trapped;
                    PrintMap(map, size);
                    return;
                }

                if (cur.Type == RoomType.DoorRoom)
                {
                    if (!door.IsLocked)
                    {
                        SetEscaped();
                        Console.WriteLine("\n=== 탈출 성공! 플레이어 승리! ===");
                        PrintMap(map, size);
                    }
                    else
                    {
                        Console.WriteLine(">>> 문이 잠겨 있습니다! 탈출 실패.");
                        State = PlayerState.Trapped;
                        PrintMap(map, size);
                        PauseForEvent(); // 잠긴 문일 때만 확인 대기
                    }
                    return;
                }

                PrintMap(map, size);
            }
        }

        // --- Stage 2 ---
        public void StartStage2(TimeLimit timeLimit)
        {
            const int size = 5;
            Console.WriteLine($"\n=================================");
            Console.WriteLine($"   Stage 2 설정 (맵 크기: {size}x{size})");
            Console.WriteLine($"=================================");

            var map = new Room[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    map[y, x] = new Room(RoomType.SafeRoom, x, y);

            map[0, 0].Type = RoomType.StartRoom;
            map[3, 4].Type = RoomType.DoorRoom;
            Door realDoor = new Door(3, 4, isFake: false);

            _playerX = 0; _playerY = 0;
            Console.WriteLine("\n[현재 고정 배치 상태 미니맵]");
            PrintMap(map, size);

            var (hintR, hintC) = InputPosition("힌트(H)", size, map);
            map[hintR, hintC].Type = RoomType.HintRoom;

            var (trapR, trapC) = InputPosition("함정(X)", size, map);
            map[trapR, trapC].Type = RoomType.TrapRoom;
            Trap trap = new Trap("방향반전함정", trapR, trapC);

            var (fakeR, fakeC) = InputPosition("가짜문(F)", size, map);
            map[fakeR, fakeC].Type = RoomType.DoorRoom;
            Door fakeDoor = new Door(fakeR, fakeC, isFake: true);

            var es2 = new Stage2ItemEventSystem(hintR, hintC, realDoor.Row, realDoor.Col);

            Console.WriteLine("\n=== Stage 2 시뮬레이션 시작 ===");
            PrintMap(map, size);
            map[0, 0].Enter();

            timeLimit.Start();

            while (State == PlayerState.Alive)
            {
                System.Threading.Thread.Sleep(1000);
                Console.Clear();

                Console.WriteLine($"[제한 시간 소요 중] 남은 시간: {Math.Max(0, timeLimit.TimeLimitSeconds - timeLimit.ElapsedSeconds)}초");

                AutoMove(map, size);
                Console.WriteLine($"\n플레이어 이동 → ({_playerX + 1},{_playerY + 1})");

                Room cur = map[_playerY, _playerX];
                cur.Enter();

                if (es2.CheckHintEvent(this, _playerY, _playerX))
                {
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (trap.IsOnSameCell(_playerY, _playerX) && trap.State == TrapState.Active)
                {
                    trap.Trigger();
                    TrapCount++;
                    Console.WriteLine("[함정] 방향이 반전됩니다!");
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (cur.Type == RoomType.DoorRoom)
                {
                    bool isReal = (_playerX == realDoor.Col && _playerY == realDoor.Row);

                    if (isReal)
                    {
                        bool success = es2.AutoPasswordEvent();
                        if (success)
                        {
                            timeLimit.Stop();
                            SetEscaped();
                            Console.WriteLine("\n=== 탈출 성공! 플레이어 승리! ===");
                            PrintMap(map, size);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("[AI] 비밀번호 모름. 계속 이동합니다.");
                            PrintMap(map, size);
                            PauseForEvent();
                            continue;
                        }
                    }
                    else if (_playerX == fakeDoor.Col && _playerY == fakeDoor.Row)
                    {
                        Console.WriteLine("\n가짜 문! 시작 위치로 이동 + 체력 -1");
                        TakeDamage(1);
                        TrapCount++;
                        _playerX = 0; _playerY = 0;

                        if (Health <= 0)
                        {
                            timeLimit.Stop();
                            Console.WriteLine("\n=== HP 소진! 탈출 실패! 설계자 승리! ===");
                            PrintMap(map, size);
                            PauseForEvent();
                            return;
                        }
                        PrintMap(map, size);
                        PauseForEvent();
                        continue;
                    }
                }

                PrintMap(map, size);
            }

            timeLimit.Stop();
            Console.WriteLine("\n=== 탈출 실패! 설계자 승리! ===");
            PrintMap(map, size);
        }

        // --- Stage 3 ---
        public void StartStage3(TimeLimit timeLimit)
        {
            const int size = 9;
            Console.WriteLine($"\n=================================");
            Console.WriteLine($"   Stage 3 설정 (맵 크기: {size}x{size})");
            Console.WriteLine($"=================================");

            var map = new Room[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    map[y, x] = new Room(RoomType.SafeRoom, x, y);

            map[0, 0].Type = RoomType.StartRoom;
            map[7, 8].Type = RoomType.DoorRoom;
            Door realDoor = new Door(7, 8, isFake: false);

            _playerX = 0; _playerY = 0;
            Console.WriteLine("\n[현재 고정 배치 상태 미니맵]");
            PrintMap(map, size);

            var (hintR, hintC) = InputPosition("힌트(H)", size, map);
            map[hintR, hintC].Type = RoomType.HintRoom;

            var (hammerR, hammerCol) = InputPosition("망치 아이템(I)", size, map);
            map[hammerR, hammerCol].Type = RoomType.ItemRoom;

            var (healR, healC) = InputPosition("힐템 아이템(H)", size, map);
            map[healR, healC].Type = RoomType.ItemRoom;

            var (fakeR, fakeC) = InputPosition("가짜문(F)", size, map);
            map[fakeR, fakeC].Type = RoomType.DoorRoom;
            Door fakeDoor = new Door(fakeR, fakeC, isFake: true);

            Console.WriteLine("\n[함정 4종류 순차 배치 진행]");
            var (t1R, t1C) = InputPosition("방향반전함정", size, map);
            map[t1R, t1C].Type = RoomType.TrapRoom;
            var trapReverse = new Trap("방향반전함정", t1R, t1C);

            var (t2R, t2C) = InputPosition("순간이동함정", size, map);
            map[t2R, t2C].Type = RoomType.TrapRoom;
            var trapTeleport = new TeleportTrap(t2R, t2C, size);

            var (t3R, t3C) = InputPosition("체력감소함정", size, map);
            map[t3R, t3C].Type = RoomType.TrapRoom;
            var trapDamage = new DamageTrap(t3R, t3C, 3);

            var (t4R, t4C) = InputPosition("시간감소함정", size, map);
            map[t4R, t4C].Type = RoomType.TrapRoom;
            var trapTime = new TimeTrap(t4R, t4C, 5);

            var es3 = new Stage3ItemEventSystem(hammerR, hammerCol, healR, healC, hintR, hintC);

            Console.WriteLine("\n=== Stage 3 시뮬레이션 시작 ===");
            PrintMap(map, size);
            map[0, 0].Enter();

            timeLimit.Start();

            while (State == PlayerState.Alive)
            {
                System.Threading.Thread.Sleep(1000);
                Console.Clear();

                Console.WriteLine($"[제한 시간 소요 중] 남은 시간: {Math.Max(0, timeLimit.TimeLimitSeconds - timeLimit.ElapsedSeconds)}초");

                AutoMove(map, size);
                Console.WriteLine($"\n플레이어 이동 → ({_playerX + 1},{_playerY + 1})");

                Room cur = map[_playerY, _playerX];
                cur.Enter();

                if (es3.CheckItemEvent(this, _playerY, _playerX, map))
                {
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (trapReverse.IsOnSameCell(_playerY, _playerX) && trapReverse.State == TrapState.Active)
                {
                    bool destroyed = es3.TryDestroyTrap(this, trapReverse, map);
                    if (!destroyed)
                    {
                        trapReverse.Trigger();
                        TrapCount++;
                        Console.WriteLine("[함정] 방향 반전!");
                    }
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (trapTeleport.IsOnSameCell(_playerY, _playerX) && trapTeleport.State == TrapState.Active)
                {
                    bool destroyed = es3.TryDestroyTrap(this, trapTeleport, map);
                    if (!destroyed)
                    {
                        trapTeleport.Trigger(ref _playerX, ref _playerY);
                        TrapCount++;
                    }
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (trapDamage.IsOnSameCell(_playerY, _playerX) && trapDamage.State == TrapState.Active)
                {
                    bool destroyed = es3.TryDestroyTrap(this, trapDamage, map);
                    if (!destroyed)
                    {
                        trapDamage.Trigger(this);
                        TrapCount++;
                        if (Health <= 0)
                        {
                            timeLimit.Stop();
                            Console.WriteLine("\n=== HP 소진! 탈출 실패! 설계자 승리! ===");
                            PrintMap(map, size);
                            PauseForEvent();
                            return;
                        }
                    }
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (trapTime.IsOnSameCell(_playerY, _playerX) && trapTime.State == TrapState.Active)
                {
                    bool destroyed = es3.TryDestroyTrap(this, trapTime, map);
                    if (!destroyed)
                    {
                        trapTime.Trigger(timeLimit);
                        TrapCount++;
                    }
                    PrintMap(map, size);
                    PauseForEvent();
                    continue;
                }

                if (cur.Type == RoomType.DoorRoom)
                {
                    bool isReal = (_playerX == realDoor.Col && _playerY == realDoor.Row);

                    if (isReal)
                    {
                        timeLimit.Stop();
                        SetEscaped();
                        Console.WriteLine("\n=== 탈출 성공! 플레이어 승리! ===");
                        PrintMap(map, size);
                        return;
                    }
                    else if (_playerX == fakeDoor.Col && _playerY == fakeDoor.Row)
                    {
                        Console.WriteLine("\n가짜 문! 시작 위치로 이동 + 체력 -3");
                        TakeDamage(3);
                        TrapCount++;
                        Console.WriteLine("\nEnter를 누르면 계속...");
                        Console.ReadLine();
                        _playerX = 0; 
                        _playerY = 0;

                        if (Health <= 0)
                        {
                            timeLimit.Stop();
                            Console.WriteLine("\n=== HP 소진! 탈출 실패! 설계자 승리! ===");
                            PrintMap(map, size);
                            PauseForEvent();
                            return;
                        }
                        PrintMap(map, size);
                        PauseForEvent();
                        continue;
                    }
                }

                PrintMap(map, size);
            }

            timeLimit.Stop();
            Console.WriteLine("\n=== 탈출 실패! 설계자 승리! ===");
            PrintMap(map, size);
        }

        private void AutoMove(Room[,] map, int size)
        {
            var moves = new List<(int x, int y)>();

            if (_playerY > 0 && !map[_playerY - 1, _playerX].IsVisited) moves.Add((_playerX, _playerY - 1));
            if (_playerY < size - 1 && !map[_playerY + 1, _playerX].IsVisited) moves.Add((_playerX, _playerY + 1));
            if (_playerX > 0 && !map[_playerY, _playerX - 1].IsVisited) moves.Add((_playerX - 1, _playerY));
            if (_playerX < size - 1 && !map[_playerY, _playerX + 1].IsVisited) moves.Add((_playerX + 1, _playerY));

            if (moves.Count == 0)
            {
                if (_playerY > 0) moves.Add((_playerX, _playerY - 1));
                if (_playerY < size - 1) moves.Add((_playerX, _playerY + 1));
                if (_playerX > 0) moves.Add((_playerX - 1, _playerY));
                if (_playerX < size - 1) moves.Add((_playerX + 1, _playerY));
            }

            int idx = _random.Next(moves.Count);
            _playerX = moves[idx].x;
            _playerY = moves[idx].y;
        }

        private void PrintMap(Room[,] map, int size)
        {
            Console.WriteLine($"\n--- 미니맵 (HP: {Health}/{MaxHealth}) ---");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x == _playerX && y == _playerY)
                        Console.Write("[P] ");
                    else
                    {
                        switch (map[y, x].Type)
                        {
                            case RoomType.StartRoom: Console.Write("[S] "); break;
                            case RoomType.HintRoom: Console.Write("[H] "); break;
                            case RoomType.DoorRoom: Console.Write("[D] "); break;
                            case RoomType.TrapRoom: Console.Write("[X] "); break;
                            case RoomType.ItemRoom: Console.Write("[I] "); break;
                            default: Console.Write("[ ] "); break;
                        }
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine(new string('-', size * 4));
        }
    }

    //  10. Ending
    public class Ending
    {
        private readonly bool _playerEscaped;
        private readonly int _trapCount;
        private readonly int _currentHp;
        private readonly int _maxHp;

        public Ending(bool playerEscaped, int trapCount, int currentHp, int maxHp)
        {
            _playerEscaped = playerEscaped;
            _trapCount = trapCount;
            _currentHp = currentHp;
            _maxHp = maxHp;
        }

        public void ShowEnding()
        {
            Console.WriteLine("\n===== GAME RESULT =====");
            if (_playerEscaped)
            {
                Console.WriteLine("탈출자 탈출 성공!");
                Console.WriteLine("관리자 패배...");
            }
            else
            {
                Console.WriteLine("탈출자 탈출 실패!");
                Console.WriteLine("관리자 승리!");
            }

            Rank rank = CalculateRank();

            Console.WriteLine("\n===== PERFECT SCORE =====");
            Console.WriteLine($"  트랩 밟은 횟수 : {_trapCount}회");
            Console.WriteLine($"  남은 체력      : {_currentHp}/{_maxHp}");
            Console.WriteLine($"  최종 등급      : {rank}");
            Console.WriteLine("=========================");
        }

        private Rank CalculateRank()
        {
            double hpRate = (double)_currentHp / _maxHp;
            if (_trapCount == 0 && _currentHp == _maxHp) return Rank.S;
            if (_trapCount <= 1 && hpRate >= 0.8) return Rank.A;
            if (_trapCount <= 2 && hpRate >= 0.6) return Rank.B;
            if (hpRate >= 0.4) return Rank.C;
            return Rank.D;
        }
    }

    //  Main
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("스테이지를 선택하세요 (1 / 2 / 3):");
            string input = Console.ReadLine();

            Player player = new Player("Hero", 20);

            if (input == "1")
            {
                player.StartStage1();
            }
            else if (input == "2")
            {
                TimeLimit timeLimit = new TimeLimit(player, 40);
                player.StartStage2(timeLimit);
            }
            else if (input == "3")
            {
                TimeLimit timeLimit = new TimeLimit(player, 80);
                player.StartStage3(timeLimit);
            }
            else
            {
                Console.WriteLine("잘못된 입력입니다.");
                return;
            }

            bool escaped = player.State == PlayerState.Escaped;
            Ending ending = new Ending(escaped, player.TrapCount, player.Health, player.MaxHealth);
            ending.ShowEnding();

            Console.WriteLine("\n아무 키나 누르면 종료합니다.");
            Console.ReadKey();
        }
    }
}