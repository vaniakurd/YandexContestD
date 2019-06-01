using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace YandexTets
{
    class Program
    {
        static void Main( string[] args )
        {
            Stopwatch timer = new Stopwatch();
            int countReques = Convert.ToInt32( Console.ReadLine() );
            List<int> requests = new List<int>();
            for( int i = 0; i < countReques; i++ )
                requests.Add( Convert.ToInt32( Console.ReadLine() ) );

            int amaountFunc = Convert.ToInt32( Console.ReadLine() );
            Dictionary<string, List<string>> functions = new Dictionary<string, List<string>>( amaountFunc );
            Dictionary<string, int> names = new Dictionary<string, int>();
            List<List<List<string>>> blocks = new List<List<List<string>>>();
            List<string> sourceCode = new List<string>();
            Dictionary<string, List<string>> callfunc = new Dictionary<string, List<string>>();
            bool finish = false;
            var currentFunc = -1;
            bool isInBlock = false;
            int countBlock = -1;
            int numberBlock = -1;
            int maxNesting = -1;
            string nameFunc = "";
            string inputStr = "";

            while( !finish )
            {
                inputStr = Console.ReadLine().Trim();
                sourceCode.Add( inputStr );
                if( inputStr == "" )
                    continue;

                inputStr = Regex.Replace( inputStr, "[ ]+", " " );

                if( inputStr.Contains( "func " ) )
                {
                    nameFunc = inputStr.Split( ' ' )[ 1 ];

                    var newFunctiom = new List<string>();
                    var newBlock = new List<List<string>>();
                    var newException = new List<string>();
                    var newCallFunc = new List<string>();
                    callfunc.Add( nameFunc, newCallFunc );
                    blocks.Add( newBlock );
                    functions.Add( nameFunc, newFunctiom );
                    currentFunc++;
                    names.Add( nameFunc, currentFunc );
                    numberBlock = -1;
                    continue;
                }

                if( inputStr.Contains( "()" ) )
                {
                    if( isInBlock )
                        blocks[ currentFunc ][ numberBlock - maxNesting + countBlock ].Add( inputStr );

                    callfunc[ nameFunc ].Add( inputStr.Split( '(' )[ 1 ].Trim() );
                    continue;
                }

                if( inputStr.Contains( "maybethrow " ) && !isInBlock )
                {
                    functions[ nameFunc ].Add( inputStr );
                    continue;
                }


                if( inputStr.Contains( "try {" ) )
                {
                    var newBlock = new List<string>();
                    blocks[ currentFunc ].Add( newBlock );
                    countBlock++;
                    numberBlock++;
                    maxNesting++;
                    if( isInBlock )
                    {
                        blocks[ currentFunc ][ numberBlock - 1 ].Add( $"block{numberBlock}" );
                        blocks[ currentFunc ][ numberBlock ].Add( inputStr );
                    }
                    else
                    {
                        blocks[ currentFunc ][ numberBlock ].Add( inputStr );
                        functions[ nameFunc ].Add( $"block{numberBlock}" );
                    }

                    isInBlock = true;
                    continue;
                }

                if( inputStr.Contains( "} suppress" ) )
                {

                    if( isInBlock )
                        blocks[ currentFunc ][ numberBlock - maxNesting + countBlock ].Add( inputStr );
                    else
                        blocks[ currentFunc ][ numberBlock ].Add( inputStr );

                    if( --countBlock == -1 )
                    {
                        isInBlock = false;
                        maxNesting = -1;
                    }

                    continue;
                }

                if( isInBlock )
                    blocks[ currentFunc ][ numberBlock ].Add( inputStr );

                if( inputStr == "}" && currentFunc + 1 == amaountFunc )
                    finish = true;

            }
            timer.Start();
            int indexFunc = 0;
            bool isOpti = true;
            int countCallFunc = 0;
            var index = 0;
            while( isOpti )
            {
                isOpti = false;
                foreach( var func in callfunc.Where( e => e.Value.Count == countCallFunc ) )
                {
                    isOpti = true;
                    indexFunc = names[ func.Key ];
                    countBlock = blocks[ indexFunc ].Count;
                    for( int i = countBlock - 1; i > -1; i-- )
                    {
                        var currentBlock = blocks[ indexFunc ][ i ];

                        for( int j = 0; j < currentBlock.Count; j++ )
                        {
                            if( i < countBlock - 1 )
                                if( currentBlock[ j ].Contains( "block" ) )
                                    InsertBlocl( currentBlock, blocks[ indexFunc ][ i + 1 ], j );

                            if( currentBlock[ j ].Contains( "()" ) )
                            {
                                var nameCallFunc = currentBlock[ j ].Trim();
                                var indexCallFunc = names[ nameCallFunc ];
                                InsertBlocl( currentBlock, functions[ nameCallFunc ], j );
                            }
                        }

                        OptimizeTry( currentBlock );
                        var str = "";
                        if( i == 0 )
                        {
                            var function = functions[ func.Key ];
                            for( int j = 0; j < function.Count; j++ )
                            {
                                str = function[ j ];
                                if( str.Contains( "block" ) )
                                {
                                    index = Convert.ToInt32( str.Split( 'k' )[ 1 ].Trim() );
                                    InsertBlocl( function, blocks[ indexFunc ][ index ], j );
                                }
                            }
                        }
                    }
                }
                countCallFunc++;
            }

            var sourseStr = "";
            foreach( var req in requests )
            {
                sourseStr = sourceCode[ req - 1];
                if (sourseStr.Contains("maybethrow"))
                {
                    Console.WriteLine(sourseStr.Split(' ')[1]);
                    continue;
                }

                if ( sourseStr.Contains("()") && !sourseStr.Contains("{"))
                {
                    var function = functions[ sourseStr ];
                    var listExc = new List<string>();
                    foreach( var exc in function )
                        listExc.Add( exc.Split( ' ' )[ 1 ] );

                    listExc = listExc.Distinct().ToList();
                    listExc.Sort();
                    for( int i = 0; i < listExc.Count; i++ )
                        Console.Write( listExc[ i ] + " ");

                    Console.WriteLine();

                    continue;
                }
                
                Console.WriteLine();
            }


            Console.ReadKey();

        }

        static void OptimizeTry( List<string> list )
        {
            var exeptions = list[ list.Count - 1 ].Remove( 0, 10 ).Split( ',' );

            foreach( string exception in exeptions )
                list.RemoveAll( e => e == $"maybethrow {exception.Trim()}" );

            list.RemoveAt( 0 );
            list.RemoveAt( list.Count - 1 );
        }

        static void InsertBlocl( List<string> block, List<string> insertBlockblock, int index )
        {
            block.RemoveAt( index );
            int offset = 0;
            foreach( var str in insertBlockblock )
                block.Insert( index + offset++, str );
        }
    }
}
