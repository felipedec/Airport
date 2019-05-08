using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Airport {
   public class ConsoleMode {
      static readonly ConfigVar<int> s_Debug = ConfigVar.CreateRangeInt("debug", "Modo debug.", 0, 0, 1);

      static bool s_IsConsoleOpen;
      static List<string> s_CommandHistory = new List<string>();
      static Regex s_DefinitionRegex = new Regex("%((cvar:)?([A-z0-9_]+))%");
      static Dictionary<string, string> s_Keys = new Dictionary<string, string>();
      static TextWriter s_StandardOut = Console.Out;
      static LinkedList<(float Time, string Command)> s_PendingTasks = new LinkedList<(float Time, string Command)>();

      public static void Define(string Key, string Value) {
         s_Keys[Key.ToLower()] = Value;
      }

      public static string Evaluate(string Text) {
         if (string.IsNullOrEmpty(Text)) {
            return Text;
         }

         var Matches = s_DefinitionRegex.Matches(Text);

         if (Matches.Count == 0) {
            return Text;
         }

         var StringBuilder = new StringBuilder(Text.Length * 2);
         int Index = 0;

         for (int MatchIndex = 0; MatchIndex < Matches.Count; MatchIndex++) {
            var Match = Matches[MatchIndex];

            StringBuilder.Append(Text, Index, Match.Index - Index);

            var Key = Match.Groups[3].Value;

            if (Match.Groups[2].Success && ConfigVar.Search(Key, out var Result)) {
               StringBuilder.Append(Result.Get());
            }
            else if (s_Keys.TryGetValue(Key.ToLower(), out var Value)) {
               StringBuilder.Append(Value);
            }
            else {
               StringBuilder.Append(Match.Value);
            }

            Index = Match.Index + Match.Length;
         }

         if (Index < Text.Length) {
            StringBuilder.Append(Text, Index, Text.Length - Index);
         }

         return StringBuilder.ToString();
      }

      [ConfigVarCommand("color", "Trocar cor do console.", false)]
      public static void SetColor(string[] Args) {
         if (Args.Length == 1 && Enum.TryParse(Args[0], true, out ConsoleColor Color)) {
            Console.ForegroundColor = Color;
         }
      }

      [ConfigVarCommand("reset_color", "Resetar cor do console", false)]
      public static void ResetColor(string[] Args) {
         Console.ResetColor();
      }


      [ConfigVarCommand("set", "Definir uma chave.", false)]
      public static void SetKey(string[] Args) {
         if (Args.Length != 2) {
            Console.WriteLine("Uso: set <chave> <valor>");
         }
         else {
            string Key = $"%{Args[0]}%";

            if (s_DefinitionRegex.IsMatch(Key)) {
               string Value = Evaluate(Args[1]);

               Define(Args[0], Value);

               Console.WriteLine($"{Args[0]} = \"{Value}\"");
            }
            else {
               Console.WriteLine("Chave inválida, são permitidos apenas letras, números e o caractério '_'.");
            }
         }
      }

      [ConfigVarCommand("echo", "Exibir mensagem.")]
      public static void Echo(string[] Args) {
         if (Args.Length > 0) {
            var PreviousOut = Console.Out;
            Console.SetOut(s_StandardOut);

            Console.WriteLine(Args[0]);

            Console.SetOut(PreviousOut);
         }
      }

      [ConfigVarCommand("set_title", "Exibir comandos disponíveis.")]
      public static void SetTitle(string[] Args) {
         if (Args.Length == 0) {
            Console.WriteLine("Uso: set_title <titulo>");
         }
         else {
            Console.Title = Args[0];
         }
      }

      [ConfigVarCommand("help", "Exibir comandos disponíveis.")]
      public static void Help(string[] Args) {
         var ConfigVars = ConfigVar.ConfigVars;

         string Table = ConfigVars.ToStringTable(new string[] { "Comando", "Tipo", "Tipo Dado", "Descrição" },
            C => C.Command, C => C.IsCommand ? "Comando" : C.IsReadOnly ? "Variável (L)" : "Variável", C => C.TypeName, C => C.Description);

         Console.WriteLine(Table);
      }

      [ConfigVarCommand("exit", "Sair do console.")]
      public static void Exit(string[] Args) {
         s_IsConsoleOpen = false;
      }

      [ConfigVarCommand("console", "Abrir o cosole.")]
      public static void ExecuteConsoleCommand(string[] Args) {
      }

      [ConfigVarCommand("set_task", "Agendar uma tarefa.")]
      public static void SetTask(string[] Args) {
         if (Args.Length == 0) {
            Console.WriteLine("Uso: set_task <time> <command>");

            return;
         }

         if (Args.Length != 2) {
            Console.WriteLine("Número de argumentos inválido.");

            return;
         }

         if (float.TryParse(Args[0], out float Time)) {
            if (Time <= 0.0f) {
               Console.WriteLine("O tempo deve ser positivo.");

               return;
            }

            float TaskTime = Simulation.Time + Time;

            var Task = (TaskTime, Args[1]);
            var Node = s_PendingTasks.First;

            if (Node == null) {
               s_PendingTasks.AddFirst(Task);

               return;
            }

            while (Node != null && Node.Value.Time < TaskTime) {
               Node = Node.Next;
            }

            if (Node == null) {
               s_PendingTasks.AddLast(Task);
            }
            else {
               s_PendingTasks.AddBefore(Node, Task);
            }

         }
         else {
            Console.WriteLine("Tempo inválido.");
         }
      }

      [ConfigVarCommand("exec", "Executar um arquivo com comandos.")]
      public static void ExecuteCommandFile(string[] Args) {
         if (Args.Length == 0) {
            Console.WriteLine("Uso: exec <file> [optional]");

            return;
         }

         bool DisplayError = !(Args.Length > 1 && Args[1].Equals("optional"));

         string Line;

         string FilePath = Path.ChangeExtension(Args[0], "txt");
         if (File.Exists(FilePath)) {
            var Previous = Console.Out;

            if (s_Debug == 0) {
               Console.SetOut(new StringWriter());
            }

            var FileReader = new StreamReader(FilePath, Encoding.GetEncoding("iso-8859-1"));

            while ((Line = FileReader.ReadLine()) != null) {
               try {
                  Process(Line);
               }
               catch (Exception Exception) {
                  Console.WriteLine($"Erro: \"{Line}\" ({Exception.Message})");
               }
            }

            Console.SetOut(Previous);
         }
         else if (DisplayError) {
            Console.WriteLine($"Arquivo \"{FilePath}\" não encontrado.");
         }

      }

      public static void ExecuteTasks() {
         var Node = s_PendingTasks.First;

         float Time = Simulation.Time;

         while (Node != null) {
            var Task = Node.Value;

            if (Task.Time > Time) {
               break;
            }

            var CompleteTaskNode = Node;
            Node = Node.Next;

            Process(Task.Command);
            s_PendingTasks.Remove(CompleteTaskNode);
         }
      }

      public static void Open() {
         if (s_IsConsoleOpen) {
            return;
         }

         Simulation.Pause();

         s_StandardOut = Console.Out;
         s_IsConsoleOpen = true;

         Console.ForegroundColor = ConsoleColor.Yellow;

         int Width = Console.WindowWidth;
         Width -= Width % 2;

         string Title = "Console Habilitado";
         string HorizontalLine = new string('─', Width - 2);
         string BottomLine = $"└{HorizontalLine}┘";
         string TopLine = $"┌{HorizontalLine}┐";

         int SpaceWidth = (Width - 2 - Title.Length) / 2;
         string Space = new string(' ', SpaceWidth);

         Console.WriteLine(TopLine);
         Console.WriteLine($"│{Space}{Title}{Space}│");
         Console.WriteLine(BottomLine);


         while (s_IsConsoleOpen) {
            try {
               string Line = ConsoleUtility.ReadLine(ConfigVar.Commands, s_CommandHistory);

               if (!string.IsNullOrWhiteSpace(Line)) {
                  s_CommandHistory.Add(Line);
               }

               Process(Line);
            } 
            catch (Exception Exception) {
               Console.WriteLine($"Erro: {Exception.Message}");
            }
         }

         Title = "Console Desabilitado";
         SpaceWidth = (Width - 2 - Title.Length) / 2;
         Space = new string(' ', SpaceWidth);

         Console.WriteLine(TopLine);
         Console.WriteLine($"│{Space}{Title}{Space}│");
         Console.WriteLine(BottomLine);

         Console.ResetColor();

         Simulation.Resume();
      }

      public static void Process(string Line) {
         if (string.IsNullOrWhiteSpace(Line)) {
            return;
         }

         int CommentIndex = Line.IndexOf('#');
         int LineLength = Line.Length;

         if (CommentIndex >= 0) {
            LineLength = CommentIndex;
         }

         int CommandLength = 0;

         while (CommandLength < LineLength && (char.IsLetterOrDigit(Line[CommandLength]) || Line[CommandLength] == '_')) {
            CommandLength++;
         }

         if (CommandLength == 0) {
            return;
         }

         string Command = Line.Substring(0, CommandLength);

         int SearchResult = ConfigVar.Search(Command);
         var ConfigVars = ConfigVar.ConfigVars;


         if (SearchResult < 0) {
            Console.WriteLine($"Commando Desconhecido: {Command}");
         }
         else {
            var ConfigVar = ConfigVars[SearchResult];

            int ArgsLength = LineLength - CommandLength;

            if (ArgsLength > 0) {
               string ArgsString = Line.Substring(CommandLength, ArgsLength);

               if (!string.IsNullOrWhiteSpace(ArgsString)) {
                  var Args = ConsoleUtility.Tokenize(ArgsString);

                  if (ConfigVar.Evaluate) {
                     for (int Index = 0; Index < Args.Length; Index++) {
                        Args[Index] = Evaluate(Args[Index]);
                     }
                  }

                  ConfigVar.Set(Args);

                  try {

                  }
                  catch (Exception Exception) {
                     Console.WriteLine($"Entrada inválida: {Exception.Message}");
                  }

                  return;
               }
            }

            string Result = ConfigVar.Get();
            
            if (!ConfigVar.IsCommand) {
               Console.WriteLine($"\"{ConfigVar.Command}\" = \"{Result}\"");
            }
         }
      }
   }
}
