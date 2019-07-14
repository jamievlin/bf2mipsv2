using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bf2mipsv2
{
    struct Args {
        public Args(string file, string outname) {
            this.filename = file; this.outputname = outname;
        }
        public string filename;
        public string outputname;
    }

    class Program
    {
        static Args parseargs(string[] args) {
            char mode = 'd';
            string filename = null;
            string outname = null;
            foreach (string arg in args) {
                switch (mode) {
                    case 'd': // searching for file
                        if (arg == "-f" || arg == "--file") {
                            mode = 'f';
                        } else if (arg == "-o" || arg == "--outfile") {
                            mode = 'o';
                        } else {
                            throw new ArgumentException("Invalid Option!");
                        }
                        break;
                    case 'f': // filename
                        filename = arg;
                        mode = 'd';
                        break;
                    case 'o': // outname
                        outname = arg;
                        mode = 'd';
                        break;
                    default:
                        break;
                }
            }

            if (mode != 'd') {
                throw new ArgumentException("Missing Argument!");
            }

            return new Args(filename, outname);
        }
        static string getBracketDepth(IEnumerable<int> bracketdat) {
            var bracketstr = 
                from i in bracketdat
                select i.ToString();

            return String.Join("_", bracketstr);
        }

        static int processDataAdjust(StreamReader reader) {
            // assumption: latest character is + or -
            // postcond: reader points to the next non-+/- character
            int moveamt = 0;
            int inst = reader.Peek();
            while (inst == '+' || inst == '-') {
                moveamt = (inst == '+') ? (moveamt + 1) : (moveamt - 1);
                reader.Read();
                inst = reader.Peek();
            }

            return moveamt; // at this point, reader points to the next non-</>
        }

        static int processPointerMove(StreamReader reader) {
            // assumption: reader latest character is < or >.
            int moveamt = 0;
            int inst = reader.Peek();
            while (inst == '<' || inst == '>') {
                moveamt = (inst == '>') ? (moveamt + 1) : (moveamt - 1);
                reader.Read();
                inst = reader.Peek();
            }

            return moveamt; // at this point, reader points to the next non-</>
        }

        static string processBf(StreamReader reader, int memory=2500) {
            StringBuilder sb = new StringBuilder();
            var bracketDepth = new Stack<int>();
            bracketDepth.Push(0);

            sb.AppendLine(".data");
            sb.AppendFormat("bf_arr: .byte {0:d}\n", memory);
            sb.AppendLine(".text");
            sb.AppendLine("main:");
            sb.AppendLine("  la $t0 bf_arr");
            sb.AppendFormat("  addi $t0 $t0 {0:d}\n", memory/2);

            while (true) {
                int inst = reader.Peek();
                if (inst == -1) { break; }

                if (inst == '+' || inst == '-') {
                    int dataAdjAmount = processDataAdjust(reader);
                    sb.AppendFormat("  lb $t1 0($t0) # inst: +/-x{0:d}\n", dataAdjAmount);
                    sb.AppendFormat("  addi $t1 $t1 {0:d}\n", dataAdjAmount);
                    sb.AppendLine("  sb $t1 0($t0)");
                } else if (inst == '<' || inst == '>') {
                    sb.AppendFormat("  addi $t0 $t0 {0:d} # inst: </>x{0:d}\n", processPointerMove(reader));
                } else if (inst == '.') {
                    sb.AppendLine("  addi $v0 $zero 11 # inst: . (cout << mem[dat];)");
                    sb.AppendLine("  lb $a0 0($t0)");
                    sb.AppendLine("  syscall");
                    reader.Read();
                } else if (inst == ',') {
                    sb.AppendLine("  addi $v0 $zero 12 # inst: , (cin >> mem[dat];)");
                    sb.AppendLine("  syscall");
                    sb.AppendLine("  sb $v0 0($t0)");
                    reader.Read();
                } else if (inst == '[') {
                    string bracketDepthStr = getBracketDepth(bracketDepth);
                    sb.AppendLine("  lb $t1 0($t0) # inst: [");
                    sb.AppendFormat("  beq $t1 $zero cl_b_{0}\n", bracketDepthStr);
                    sb.AppendFormat("op_b_{0:s}:\n", bracketDepthStr);
                    bracketDepth.Push(0);
                    reader.Read();
                } else if (inst == ']') {
                    bracketDepth.Pop();
                    string bracketDepthStr = getBracketDepth(bracketDepth);
                    sb.AppendLine("  lb $t1 0($t0) # inst: ]");
                    sb.AppendFormat("  bne $t1 $zero op_b_{0:s}\n", bracketDepthStr);
                    sb.AppendFormat("cl_b_{0:s}:\n", bracketDepthStr);
                    bracketDepth.Push(bracketDepth.Pop() + 1);
                    reader.Read();
                } else {
                    reader.Read();
                }
            }
            sb.AppendLine("  jr $ra # ret");

            return sb.ToString();
        }

        static void Main(string[] args)
        {
            Args arg = parseargs(args);
            string prog;
            using (var fileReader = new StreamReader(arg.filename)) {
                prog = processBf(fileReader);
            }
            Console.WriteLine(prog);
        }
    }
}
