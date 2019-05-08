using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Airport {
   public static class ConsoleUtility {
      public static void WriteLine(string Text, ConsoleColor ForegroundColor = ConsoleColor.Gray) {
         var PreviousForegroundColor = Console.ForegroundColor;

         Console.ForegroundColor = ForegroundColor;

         Console.WriteLine(Text);

         Console.ForegroundColor = PreviousForegroundColor;
      }

      public static void Write(string Text, ConsoleColor ForegroundColor = ConsoleColor.Gray) {
         var PreviousForegroundColor = Console.ForegroundColor;

         Console.ForegroundColor = ForegroundColor;

         Console.Write(Text);

         Console.ForegroundColor = PreviousForegroundColor;
      }

      public static string ReadLine(List<string> Completes, List<string> History, ConsoleColor Color = ConsoleColor.Gray, ConsoleColor SelectionColor = ConsoleColor.DarkGray, bool AllowEmptyLine = false) {
         var PreviousColor = Console.ForegroundColor;

         Console.ForegroundColor = Color;

         var Input = new StringBuilder(128);

         int Left = Console.CursorLeft;
         int Top = Console.CursorTop;

         string CachedEmptyLine = new string(' ', Math.Max(Console.WindowWidth - Left, Console.WindowWidth));

         string CachedInput = null;
         int Offset = 0;
         int PreviousOffset = 0;

         while (true) {
            var Key = Console.ReadKey(true);

            if (Key.Key == ConsoleKey.Enter) {
               if (Offset != 0) {
                  Reset();
               }
               else if (IsEmpty() ^ !AllowEmptyLine) {
                  break;
               }
            }

            if (char.IsControl(Key.KeyChar)) {
               switch (Key.Key) {
                  case ConsoleKey.Backspace:
                     if (Offset > 0) {
                        Offset = 0;
                     }
                     else {
                        Reset();
                     }

                     if (Input.Length > 0) {
                        if (Key.Modifiers == ConsoleModifiers.Control) {
                           while (Input.Length > 0) {
                              char Char = Input[Input.Length - 1];

                              Input.Length--;

                              if (char.IsWhiteSpace(Char) || Char == ',' || Char == '_' || Char == '.') {
                                 break;
                              }
                           }
                        }
                        else {
                           Input.Length--;
                        }
                     }

                     break;
                  case ConsoleKey.RightArrow:
                     Reset();

                     break;
                  case ConsoleKey.UpArrow:
                     Offset--;

                     break;
                  case ConsoleKey.DownArrow:
                     Offset++;

                     break;
                  case ConsoleKey.Tab:
                     if (Key.Modifiers == ConsoleModifiers.Shift) {
                        Offset--;
                     }
                     else {
                        Offset++;
                     }

                     break;
                  case ConsoleKey.Escape:
                     Reset();

                     break;
               }
            }
            else {
               Reset();

               Input.Append(Key.KeyChar);
               Console.Write(Key.KeyChar);

            }

            if (PreviousOffset != Offset) {
               if (PreviousOffset == 0) {
                  CachedInput = Input.ToString();
               }

               if (Offset == 0) {
                  SetInput(CachedInput);
               }

               if (Offset > 0) {
                  int Index = Completes.BinarySearch(CachedInput, new ConfigVarComparer());

                  if (Index < 0) {
                     Index = ~Index;
                  }

                  Index--;

                  if (Index + Offset >= Completes.Count) {
                     Offset = Completes.Count - Index - 1;

                     if (Offset == 0) {
                        break;
                     }
                  }

                  while (!Completes[Index + Offset].StartsWith(CachedInput, StringComparison.OrdinalIgnoreCase)) {
                     Offset--;

                     if (Offset == 0) {
                        break;
                     }
                  }

                  if (Offset != PreviousOffset) {
                     SetInput(Completes[Index + Offset]);
                  }
               }
               else {
                  int AbsOffset = Math.Abs(Offset);

                  AbsOffset = Math.Min(History.Count, AbsOffset);

                  if (AbsOffset > 0) {
                     Offset = -AbsOffset;
                     SetInput(History[History.Count - Math.Abs(Offset)]);
                  }
                  else {
                     Offset = 0;
                  }
               }
            }

            Clear();

            Console.CursorVisible = Offset == 0;

            if (Offset > 0) {
               int LeftOffset = CachedInput.Length;

               Console.Write(CachedInput);

               Console.SetCursorPosition(Left + LeftOffset, Top);
               Selection(() => Console.Write(Input.ToString(CachedInput.Length, Input.Length - LeftOffset), ConsoleColor.DarkGray));
               Console.SetCursorPosition(Left + LeftOffset, Top);

            }
            else {
               if (Offset < 0) {
                  Selection(() => Console.Write(Input));
               }
               else {
                  Console.Write(Input);
               }

               Console.SetCursorPosition(Left + Input.Length, Top);
            }

            PreviousOffset = Offset;
         }

         Console.WriteLine();
         Console.ForegroundColor = PreviousColor;

         return Input.ToString();

         void SetInput(string Text) {
            Input.Length = 0;
            Input.Append(Text);
         }

         bool IsEmpty() {
            for (int Index = 0; Index < Input.Length; Index++) {
               if (!char.IsWhiteSpace(Input[Index])) {
                  return false;
               }
            }
            return true;
         }

         void Reset() {
            Offset = 0;

            PreviousOffset = Offset;

            Clear();
            Console.Write(Input);
         }

         void Clear() {
            Console.SetCursorPosition(Left, Top);
            Console.Write(CachedEmptyLine);
            Console.SetCursorPosition(Left, Top);
         }

         void Selection(Action Inside) {
            var TmpBackgroundColor = Console.BackgroundColor;

            Console.BackgroundColor = SelectionColor;
            Console.ForegroundColor = ConsoleColor.Black;

            Inside();

            Console.ForegroundColor = Color;
            Console.BackgroundColor = TmpBackgroundColor;
         }
      }

      static Regex s_TokenizeRegex = new Regex("[\"\"].+?[\"\"]|[^\t ]+");

      public static string[] Tokenize(string Text) {
         if (string.IsNullOrWhiteSpace(Text)) {
            return Array.Empty<string>();
         }
 
         var Matches = s_TokenizeRegex.Matches(Text);
         var Result = new string[Matches.Count];

         for (int Index = 0; Index < Matches.Count; Index++) {
            var Match = Matches[Index];

            Result[Index] = Match.Value.Trim(' ').Trim('"').Replace('\'', '\"');
         }
         return Result;
      }
   }
}

