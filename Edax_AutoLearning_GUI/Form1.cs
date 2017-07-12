using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Edax_AutoLearning_GUI
{
    public partial class Form1 : Form
    {
        string Edax_Path = Path.GetDirectoryName(Application.ExecutablePath) + MyString.EXE_edax;
        public delegate void MyEventHandler(object sender, DataReceivedEventArgs e);
        public event MyEventHandler myEvent = null;
        Process edax_process = null;

        //学習中or停止中のparam
        Boolean is_learning = false;

        //"Game Over"の出現カウント(mode 2の終了タイミング判定のため)
        int count_GameOver = 0;

        //"to do"の出現カウント(Book deviate x y...を体裁よく出力するため)
        int count_todo = 0;

        public Form1()
        {
            InitializeComponent();
            reSize_Console();
            File_check();
            if (Properties.Settings.Default.is_FirstOpen)
            {
                MessageBox.Show(MyString.FIRST_MESSAGE);
                Properties.Settings.Default.is_FirstOpen = false;

                //マニュアルを読取専用にしておく
                FileAttributes attr = File.GetAttributes(MyString.MANUAL_txt);
                File.SetAttributes(MyString.MANUAL_txt, attr | FileAttributes.ReadOnly);
            }            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosed  += new FormClosedEventHandler(Form1_FormClosed);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            set_Image();
        }

        /**
         * @brief Form1が閉じられた後の処理
         */
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.Save();
            try
            {
                if (edax_process != null)
                {
                    edax_process.Kill();
                    edax_process.Close();
                    edax_process.Dispose();
                }
            }
            catch (InvalidOperationException exc) { }
        }

        /**
         * @brief Form1を閉じようとする際の処理
         */
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (is_learning)
            {
                DialogResult result = MessageBox.Show(MyString.CLOSING_MESSAGE, MyString.CLOSING_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        /**
         * @brief edaxの標準出力を見て、その内容に応じて処理
         *
         * 受け取った標準出力を、体裁よく出力
         * また、出力内容に応じて標準入力
         */
        void event_DataReceived(object sender, DataReceivedEventArgs e)
        {
            //MessageBox.Show(e.Data);
            if (e.Data == null) return;

            //------------------------------
            //外部コンソールに標準出力を転写
            //------------------------------
            if (e.Data.Contains(MyString.LINKING_BOOK))
            {   //storeにおけるLinking文字列は上書きで出力
                String data = e.Data.Replace("\r", "").Replace("\n", "");
                Console.Write(data);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            else if (e.Data.Contains(MyString.IN_DEVIATE))
            {   //deviate後の文字列の出力を調整
                Console.SetCursorPosition(0, Console.CursorTop);
                String data = e.Data.Replace("\r", "").Replace("\n", "");
                Console.Write(data);
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                count_todo = 0;
            }
            else if (e.Data.Contains(MyString.DEV_TODO))
            {   //"todo"文字を含む場合は上書きで出力
                count_todo++;
                if(count_todo == 1)
                {
                    Console.SetCursorPosition(0, Console.CursorTop + 1);
                }
                else
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
                Console.Write(e.Data + "\n");
            }
            else if (e.Data.Contains(MyString.NEGAMAXING_BOOK))
            {   //store成功後の出力を調整
                Console.Write("\n" + e.Data + "\n");
            }
            else if (e.Data.Contains(MyString.FINISHED))
            {   //finished文字列の出力を調整
                Console.Write("\r\n\r\n" + e.Data);
            }
            else
            {
                Console.WriteLine(e.Data);
            }

            //------------------------------
            //次の標準入力を判定
            //------------------------------
            if (e.Data.Contains(MyString.FINISHED))
            {   //"finished"発見後　【deviate】用
                Restart_edax();
            }
            else if (e.Data.Contains(MyString.GAME_OVER))
            {   //"Game Over"発見後 【mode 2】用
                count_GameOver++;
                if (count_GameOver == 1)
                {
                    Console.WriteLine("book storeを開始.....");
                    edax_process.StandardInput.WriteLine(MyString.BOOK_STORE);
                }
                if (count_GameOver == 2)
                {
                    count_GameOver = 0;
                    Restart_edax();
                }
            }
            else if (e.Data.Contains(MyString.BOOK_FIXED))
            {
                Restart_edax();
            }
        }

        /**
         * @brief 非同期で出力を読み込む
         *
         * 非同期読込メソッドはmainではないスレッドが呼び出すので、
         * メインフォームのコントロールにアクセスする場合はInvokeメソッドを利用して、スレッドの同期をとる必要がある
         */
        void process_DataReceived(object sender, DataReceivedEventArgs e)
        {
            this.Invoke(myEvent, new object[2] { sender, e });
        }

        /**
         * @brief Main  ボタンクリックイベント
         */
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Text = MyString.BTN_TXT_MOVING;
            button1.Enabled = false;
            Start_edax();
            MyInput();
        }

        /**
         * @brief edaxを起動
         *
         * edaxを起動し、非同期読込を開始
         */
        private void Start_edax()
        {
            Init();

            //イベントハンドラmyEventを作成
            myEvent = new MyEventHandler(event_DataReceived);

            edax_process = new Process();
            edax_process.StartInfo.FileName = Edax_Path;
            edax_process.StartInfo.UseShellExecute        = false; //ShellExecuteを使わない設定にする。BeginOutputReadLine()を利用するための条件だから。
            edax_process.StartInfo.RedirectStandardOutput = true;  // 標準出力をリダイレクト
            edax_process.StartInfo.RedirectStandardInput  = true;  // 標準入力をリダイレクト
            edax_process.StartInfo.CreateNoWindow         = true;  // edaxのコンソールを開かない
            edax_process.OutputDataReceived += new DataReceivedEventHandler(process_DataReceived); //非同期読込での完了イベントとなるイベントハンドラを設定。BeginOutputReadLine()を利用するための条件だから。

            edax_process.Start();

            //標準出力の非同期読込を開始
            edax_process.BeginOutputReadLine();

            //学習の様子を目視できるようにするに、コンソールへ出力
            Console.WriteLine(MyString.CONSOLE_TITLE);
        }

        /**
         * @brief edaxへの標準入力
         *
         * 学習予定リストから棋譜を取得し、その内容に応じて標準入力
         */
        private void MyInput()
        {
            //テキストから棋譜を取得
            MyTextOperation MyTextOperation = new MyTextOperation();
            String second_txt = MyTextOperation.Get_Second_Text();
            Second_box.Text = second_txt;
            if (second_txt == null || second_txt.Trim().Equals("")) Second_box.Text = MyString.NOTHING_TXT;

            String first_txt = MyTextOperation.Get_First_Text();
            First_box.Text = first_txt;

            if (first_txt == null || first_txt.Equals(""))
            {   //テキストファイルが空行
                is_learning = false;
                MessageBox.Show(MyString.NOTHING_TXT);
                set_received_ok();
                Console.Clear();
                return;
            }
            if (first_txt.Equals("stop"))
            {   //【stop】
                is_learning = false;
                MessageBox.Show(MyString.STOP);
                set_received_ok();
                Console.Clear();
                return;
            }

            Boolean isDeviate = first_txt.Substring(0,1)==("[");
            is_learning = true;
            if (isDeviate)
            {   //【deviate】
                //引数2つを取得 (2桁にも対応)
                int index_mark    = first_txt.IndexOf("]");
                int index_empty   = first_txt.IndexOf(" ");
                String dev_param1 = first_txt.Substring(1, index_empty - 1);
                String dev_param2 = first_txt.Substring(index_empty + 1, index_mark - 1);
                String moves      = first_txt.Substring(index_mark + 1).Trim();
                if(!is_RecordMoves(moves))
                {   //不適切な文字列の場合はSTOP
                    MessageBox.Show(MyString.TXT_ERROR);
                    set_received_ok();
                    Console.Clear();
                    return;
                }
                // >>
                //ToDo ここらでdevの可否をチェックしたい
                // <<
                edax_process.StandardInput.WriteLine(MyString.PLAY + moves);
                edax_process.StandardInput.WriteLine(MyString.BOOK_DEVIATE + dev_param1 + " " + dev_param2);
            }
            else if (first_txt.Trim().Equals("fix"))
            {   //【fix】
                Console.WriteLine("book fix を開始.....");
                edax_process.StandardInput.WriteLine(MyString.BOOK_FIX);
            }
            else
            {   //【mode 2】
                //まずランダム幅指定をチェックする
                String moves = first_txt.Trim();
                String randomness = MyInteger.randomness_init.ToString();
                Boolean is_set_random = first_txt.Contains(",");
                if (is_set_random)
                {   //幅指定あり
                    int conma_index = first_txt.IndexOf(",");
                    randomness = first_txt.Substring(0, conma_index);
                    moves = first_txt.Substring(conma_index + 1).Trim();
                }
                if(!is_RecordMoves(moves))
                {   //不適切な文字列の場合はSTOP
                    MessageBox.Show(MyString.TXT_ERROR);
                    set_received_ok();
                    Console.Clear();
                    return;
                }
                edax_process.StandardInput.WriteLine(MyString.BOOK_RANDOMNESS + randomness);
                edax_process.StandardInput.WriteLine(MyString.PLAY + moves);
                edax_process.StandardInput.WriteLine(MyString.MODE_2);
            }

        }

        /**
         * @brief edaxの再起動
         *
         * edaxを落とし、再起動する (book更新のため)
         */
        private void Restart_edax()
        {
            edax_process.StandardInput.WriteLine(MyString.EXIT);
            Console.WriteLine("bookの更新中.....");

            MyTextOperation MyTextOperation = new MyTextOperation();
            MyTextOperation.Delete_Text();

            //book更新のために、再起動まで処理を停止
            sleepAsync(MyInteger.wait_start); 

            Console.Clear();
            Start_edax();
            MyInput();
        }

        /**
         * @brief ボタン受付の許可
         *
         * stop後の処理
         */
        private void set_received_ok()
        {
            if (edax_process != null)
            {
                edax_process.Kill();
                edax_process.Close();
                edax_process.Dispose();
            }
            button1.Text = MyString.BTN_TXT_START_OK;
            button1.Enabled = true;
        }

        /**
         * @brief コンソールをリサイズ
         */
        private void reSize_Console()
        {
            Console.SetWindowSize(MyInteger.width_Consle, MyInteger.height_Consle);
        }

        /**
        * @brief テキストファイルの有無をチェック
        * 
        * 無い場合は新規作成
        */
        private void File_check()
        {
            if (!File.Exists(MyString.Will_learn_List_txt))
            {
                File.CreateText(MyString.Will_learn_List_txt);
            }
            if (!File.Exists(MyString.Had_learn_List_txt))
            {
                File.CreateText(MyString.Had_learn_List_txt);
            }
        }

        /**
         * @brief フォームの変数を初期化
         */
        private void Init()
        {
            is_learning = false;
            count_GameOver = 0;
            count_todo = 0;
            //button1.Text = MyString.BTN_TXT_START_OK;
            //button1.Enabled = true;
        }

        /**
         * @brief 非同期的な待機
         * 
         * @param wait_second  待機秒数
         */
        private async void sleepAsync(int wait_second)
        {
            await Task.Delay(wait_second * 1000);
        }

        /**
         * @brief 画像をセット
         */
        private void set_Image()
        {
            Bitmap bmp = Properties.Resources.update_s;
            pictureBox1.Image = new Bitmap(bmp);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        /**
         * @brief 更新画像のクリックイベント
         * 
         * テキスト情報を更新する
         */
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MyTextOperation myTextOperation = new MyTextOperation();
            if(First_box.Text == null || First_box.Text == "")
            {
                First_box.Text = myTextOperation.Get_First_Text();
            }
            Second_box.Text = myTextOperation.Get_Second_Text();
            if (Second_box.Text == "" || Second_box.Text == null) Second_box.Text = MyString.NOTHING_TXT;
            this.ActiveControl = null;
        }

        /**
         * @brief 棋譜テキスト化を判定
         * 
         * F5F6...の形式であるかをチェック
         * 
         * @param moves    棋譜文字列
         * @return     棋譜の形式かどうか
         */
        private Boolean is_RecordMoves(String moves)
        {
            for (int i = 0; i < moves.Length; i++) 
            {
                System.Text.RegularExpressions.Regex alpha = new System.Text.RegularExpressions.Regex(
                @"[a-hA-H]",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                System.Text.RegularExpressions.Regex num = new System.Text.RegularExpressions.Regex(
                @"[1-8]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                String s = moves.Substring(i, 1);
                if (i % 2 == 0  &&  !alpha.IsMatch(s))
                {
                    return false;
                }
                if (i % 2 == 1  &&  !num.IsMatch(s))
                {
                    return false;
                }
                
            }
            return true;
        }
    }
}
