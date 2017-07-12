using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edax_AutoLearning_GUI
{
    //文字定数を定義するクラス
    class MyString
    {
        //deviate関連
        public const String FINISHED             = "finished";
        public const String PLAY                 = "play ";
        public const String BOOK_DEVIATE         = "book deviate ";
        public const String IN_DEVIATE           = "positions, ";
        public const String BOOK_SHOW            = "book show";
        public const String DEV_TODO             = "todo";

        //mode 2 関連
        public const String MODE_2               = "mode 2"; //edax vs edax
        public const String GAME_OVER            = "Game Over";
        public const String BOOK_STORE           = "book store";
        public const String LINKING_BOOK         = "Linking book";
        public const String NEGAMAXING_BOOK      = "Negamaxing";
        public const String BOOK_RANDOMNESS      = "book-randomness ";

        //fix関連
        public const String BOOK_FIX             = "book fix";
        public const String BOOK_FIXED           = "Sorting book...done";

        //パス
        public const String Will_learn_List_txt  = @"学習予定リスト.txt";
        public const String Had_learn_List_txt   = @"学習ログ.txt";
        public const String MANUAL_txt           = @"簡易マニュアル.txt";
        public const String EXE_edax             = @"\wEdax-x64.exe";

        //メッセージ
        public const String NOTHING_TXT          = "学習対象がありません\nテキストが空行です";
        public const String STOP                 = "STOPしました";
        public const String CLOSING_MESSAGE      = "学習中です！\n\n 強制中断してウィンドウを閉じますか？";
        public const String CLOSING_TITLE        = "質問";
        public const String FIRST_MESSAGE        = "まず、簡易マニュアルを確認して導入を終えて下さい";

        //ボタン
        public const String BTN_TXT_START_OK     = "棋譜読込 + スタート";
        public const String BTN_TXT_MOVING       = "動作中";

        //その他
        public const String TITLE                = "*****出力確認用コンソール*****";
        public const String EXIT                 = "exit";
        
    }
}
