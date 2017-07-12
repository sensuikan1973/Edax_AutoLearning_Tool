using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Edax_AutoLearning_GUI
{
    //テキスト操作クラス
    class MyTextOperation
    {
        /**
         * @brief 今回学習する棋譜を取得
         *
         * 学習予定リストから今回学習する棋譜を取得する
         * 
         * @return  Trim済みの文字列
         */
        public String Get_First_Text()
        {
            using(StreamReader reader = new StreamReader(MyString.Will_learn_List_txt, Encoding.Default))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Contains("/"))
                    {
                        return line.Trim();
                    }
                }
                return line;
            }
        }

        /**
         * @brief 次回学習予定の棋譜を取得
         *
         * 学習予定リストから次回学習予定の棋譜を取得する
         * 
         * @return  Trim済みの文字列
         */
        public String Get_Second_Text()
        {
            using(StreamReader reader = new StreamReader(MyString.Will_learn_List_txt, Encoding.Default))
            {
                int count = 0;
                String line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Contains("/"))
                    {
                        count++;
                        if (count == 2) {
                            return line.Trim();
                        }

                    }
                }
                return line;
            }
        }

        /**
         * @brief 学習済みの棋譜を削除
         *
         * 学習予定リストから学習済みの棋譜を削除する
         */
        public void Delete_Text()
        {
            String[] lines = File.ReadAllLines(MyString.Will_learn_List_txt, Encoding.Default);

            using (StreamReader reader = new StreamReader(MyString.Will_learn_List_txt, Encoding.Default))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    //学習ログを残す
                    Write_log(line.Trim()); 

                    //その1行を削除
                    lines = lines.Skip(1).ToArray();

                    //非コメント文字列であれば、それを最後にしてbreak
                    if (!line.Contains("/")) break;
                }
            }
            File.WriteAllLines(MyString.Will_learn_List_txt, lines, Encoding.Default);
        }

        /**
         * @brief ログを残す
         *
         * 学習予定リストから学習済みの棋譜を学習ログへ転写する
         *
         * @param line   転写したい文字列
         */
        private void Write_log(String line)
        {
            String allText = File.ReadAllText(MyString.Had_learn_List_txt, Encoding.Default);
            String newText = line + "\r\n" + allText;
            File.WriteAllText(MyString.Had_learn_List_txt, newText, Encoding.Default);
        }

    }
}
