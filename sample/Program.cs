/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Nuno Fachada
 * */

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SampleLP2p1
{
    public class Program
    {
        // Nome da aplicação
        private const string appName = "MyIMDBSearcher";

        // Nome do ficheiro de interesse
        private const string fileTitleBasics = "title.basics.tsv.gz";

        // Número de títulos a mostrar de cada vez
        private const int numTitlesToShowOnScreen = 10;

        // Coleção de títulos
        private ICollection<Title> titles;

        // Diferentes géneros
        private ISet<string> allGenres;

        // O programa começa aqui
        private static void Main(string[] args)
        {
            Program p = new Program();
            p.ShowAnExampleOfHowThisMightWork();
        }

        // Um exemplo de como algumas coisas poderão funcionar neste projeto
        private void ShowAnExampleOfHowThisMightWork()
        {
            // Variável auxiliar usada para as pesquisas
            Title[] queryResults;

            // Número de títulos
            int numTitles = 0;

            // Número de títulos já mostrados ao utilizador
            int numTitlesShown = 0;

            // Inicializar conjunto contendo os diferentes géneros na base de
            // dados (não permite géneros repetidos)
            allGenres = new HashSet<string>();

            // Caminho completo da pasta contendo os ficheiros de dados
            string folderWithFiles = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                appName);

            // Caminho completo de cada um dos ficheiros de dados
            string fileTitleBasicsFull =
                Path.Combine(folderWithFiles, fileTitleBasics);

            // Contar número de linhas (número de títulos)
            GZipReader(fileTitleBasicsFull, (line) => numTitles++);

            // Instanciar lista com tamanho pré-definido para o número de
            // títulos existente
            titles = new List<Title>(numTitles);

            // Preencher lista de títulos com informação lida do ficheiro
            GZipReader(fileTitleBasicsFull, LineToTitle);

            // Quanta memória estamos a ocupar?
            Console.WriteLine("\t=> Program is currently occupying " +
                ((Process.GetCurrentProcess().VirtualMemorySize64) / 1024 / 1024)
                + " megabytes of memory");

            // Mostrar todos os géneros conhecidos, ordenados por eles próprios
            Console.Write($"\t=> Known genres (total {allGenres.Count}): ");
            foreach (string genre in allGenres.OrderBy(g => g))
                Console.Write($"{genre} ");
            Console.WriteLine();

            // Pesquisar por títulos cujo título contenha "video" e "game",
            // ordenando os resultados por ano e depois por título e
            // convertendo os resultados num array para depois os podermos
            // percorrer de forma eficiente
            queryResults =
                 (from title in titles
                 where title.PrimaryTitle.ToLower().Contains("video")
                 where title.PrimaryTitle.ToLower().Contains("game")
                 select title)
                 .OrderBy(title => title.StartYear)
                 .ThenBy(title => title.PrimaryTitle)
                 .ToArray();

            // Dizer quantos títulos foram encontrados
            Console.WriteLine($"\t=> There are {queryResults.Count()} titles"
                + " with \"video\" and \"game\"");

            // Mostrar os títulos, 10 de cada vez
            while (numTitlesShown < queryResults.Length)
            {
                Console.WriteLine(
                    $"\t=> Press key to see next {numTitlesToShowOnScreen} titles...");
                Console.ReadKey(true);

                // Mostrar próximos 10
                for (int i = numTitlesShown;
                    i < numTitlesShown + numTitlesToShowOnScreen
                        && i <  queryResults.Length;
                    i++)
                {
                    // Usar para melhorar a forma como mostramos os géneros
                    bool firstGenre = true;

                    // Obter titulo atual
                    Title title = queryResults[i];

                    // Mostrar informação sobre o título
                    Console.Write("\t\t* ");
                    Console.Write($"\"{title.PrimaryTitle}\" ");
                    Console.Write($"({title.StartYear?.ToString() ?? "unknown year"}): ");
                    foreach (string genre in title.Genres)
                    {
                        if (!firstGenre) Console.Write("/ ");
                        Console.Write($"{genre} ");
                        firstGenre = false;
                    }
                    Console.WriteLine();
                }

                // Próximos 10
                numTitlesShown += numTitlesToShowOnScreen;
            }
        }

        // Este método aplica uma ação (sob a forma de um delegate) a cada
        // linha de um ficheiro texto comprimido em GZip
        private static void GZipReader(
            string file, Action<string> actionForEachLine)
        {
            // Abrir ficheiro em modo leitura
            using (FileStream fs = new FileStream(
                file, FileMode.Open, FileAccess.Read))
            {
                // Decorar o ficheiro com um compressor para o formato GZip
                using (GZipStream gzs = new GZipStream(
                    fs, CompressionMode.Decompress))
                {
                    // Usar um StreamReader para simplificar a leitura
                    using (StreamReader sr = new StreamReader(gzs))
                    {
                        // Linha a ler
                        string line;

                        // Ignorar primeira linha de cabeçalho
                        sr.ReadLine();

                        // Percorrer linhas
                        while ((line = sr.ReadLine()) != null)
                        {
                            // Aplicar ação à linha atual
                            actionForEachLine.Invoke(line);
                        }
                    }
                }
            }
        }

        // Método que converte uma linha do ficheiro num título, adicionando-o
        // à coleção de títulos
        // Também processa os géneros
        private void LineToTitle(string line)
        {
            //0          1          2             3              4        5          6       7               8
            //tconst     titleType  primaryTitle  originalTitle  isAdult  startYear  endYear runtimeMinutes  genres
            //tt0000001  short      Carmencita    Carmencita     0        1894       \N      1               Documentary,Short

            short aux;
            string[] fields = line.Split("\t");
            string[] titleGenres = fields[8].Split(",");
            ICollection<string> cleanTitleGenres = new List<string>();
            short? startYear;

            // Tentar determinar ano de lançamento, se possível
            try
            {
                startYear = short.TryParse(fields[5], out aux)
                    ? (short?)aux
                    : null;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Tried to parse '{line}', but got exception '{e.Message}'"
                    + $" with this stack trace: {e.StackTrace}");
            }

            // Remover géneros inválidos
            foreach (string genre in titleGenres)
                if (genre != null && genre.Length > 0 && genre != @"\N")
                    cleanTitleGenres.Add(genre);

            // Adicionar géneros válidos ao conjunto de todos os géneros da
            // base de dados
            foreach (string genre in cleanTitleGenres)
                allGenres.Add(genre);

            // Criar novo Título usando a informação obtida da linha
            Title t = new Title(
                fields[2], startYear, cleanTitleGenres.ToArray());

            // Adicionar Título à coleção de títulos
            titles.Add(t);
        }
    }
}
