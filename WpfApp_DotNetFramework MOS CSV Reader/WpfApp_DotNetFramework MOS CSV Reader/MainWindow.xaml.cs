using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Data.SqlClient;

namespace WpfApp_DotNetFramework_MOS_CSV_Reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection connection = new SqlConnection("Data Source=DESKTOP-0BDFNSN; Database=CSV_DB; Trusted_Connection=Yes");

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ReadOneFileBtn_Click(object sender, RoutedEventArgs e)
        {
            //правя си един файл-диалогов прозорец и посочвам файла, който искам да прочете
            //тази функция прочита само един файл и създава база данни с името на файла (без разширението му)
            OpenFileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = "C:\\";
            fd.Filter = "CSV Files (*.csv)|*.csv";
            fd.Multiselect = false;
            fd.Title = "Select Single CSV File";
            if (fd.ShowDialog(this) == true)
            {
                loadIntoDb(fd.FileName, fd.SafeFileName);
            }
        }

        private void ReadMultipleBtn_Click(object sender, RoutedEventArgs e)
        {
            //същия диалогов прозорец, но може да се посочат повече от един файл
            //всеки файл се прочита и чрез цикъл, обхождаме всички прочетени файлове и работим с тях поотделно, чрез същата функция (loadIntoDb(), като за параметри на фукнцията подаваме пътя до файла и името на файла)
            //целта на параметрите е да се раздели името на файла с неговото разширение и да се създаде таблица с името на файла, и разбира се, пътя на файла е .. за местоположението на файла
            OpenFileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = "C:\\";
            fd.Filter = "CSV Files (*.csv)|*.csv";
            fd.Multiselect = true;
            fd.Title = "Select Multiple CSV Files";
            if (fd.ShowDialog(this) == true)
            {
                string AllFilesMessage = "";
                for (int i = 0; i < fd.FileNames.Length; i++)
                {
                    string[] fileN = fd.FileNames;
                    string[] safeFileN = fd.SafeFileNames;
                    loadIntoDb(fileN[i], safeFileN[i]);
                    AllFilesMessage += fileN[i] + "\n";
                }
                MessageBox.Show("Selected Files:\n" + AllFilesMessage);
            }
        }

        private void loadIntoDb(string fileName, string safeFileName)
        {
            MessageBox.Show("Selected File:\n" + fileName);
            //създавам си reader, който да чете от файла ред по ред
            StreamReader reader = new StreamReader(File.OpenRead(fileName));
            //тук извличаме само името на файла без разширението му
            string[] fileNameOnly = safeFileName.Split('.');
            //това го правя, за да отделя първия/заглавния ред на csv файла от съдържанието, като спрямо него се създава базата данни 
            string firstLine = reader.ReadLine();
            string[] firstLineValues = firstLine.Split(',');
            string columns = "";
            string columnNamesOnly = "";
            int columnsCount = 0;
            for (int i = 0; i < firstLineValues.Length; i++)
            {
                columnNamesOnly += firstLineValues[i].ToString();
                //обхождаме първия ред, като правим стринг, който ще бъде заявката за създаване на таблицата, като по този начин определяме какви колони с какви имена ще бъде таблицата
                //приемаме, че всяка една добавена колона е от тип varchar, като правим изключение за Wage (заплата), която ще бъде от тип float
                if (firstLineValues[i].ToString() != "Wage")
                {
                    columns += firstLineValues[i].ToString() + " varchar(255)";
                }
                else
                    columns += firstLineValues[i].ToString() + " float";
                //докато не бъде достигнат края на колоните се добавя запетая след всяка една "колона", за да може да се добави в заявката.
                if (i + 1 < firstLineValues.Length)
                {
                    columns += ", ";
                    columnNamesOnly += ", ";
                }
                columnsCount++;
            }
            //
            //тук слагаме вече създадения първи ред, като предварително добавяме и PersonID - идентификатор на записа
            try
            {
                connection.Open();
                string sql = "CREATE TABLE " + fileNameOnly[0] + " (ID int NOT NULL PRIMARY KEY, " + columns + ");";
                SqlCommand command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
                MessageBox.Show("Table Name: " + fileNameOnly[0] + "\nColumns:\n" + columns, "Table Created Successfully!");
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex);
                connection.Close();
            }
            //този лист го правя, за да изведа просто съдържанието на файла тук в messagebox - реално не е нужен
            List<string> row = new List<string>();
            //тук вече чете от 2ри ред - същинските записи - процеса е подобен на по-горния код с четенето ред по ред и разделянето чрез запетая
            //пак hard-coding брояча на personID започва от 1, тъй като не трябва да е 0 и е ключово поле, това е уникалния идентификатор (пореден номер) на записа (потребителя)
            try
            {
                //отваряме си връзката, задаваме начални параметри на заявката и командата и започваме да обхождаме файла
                connection.Open();
                string sql = "";
                SqlCommand command;
                int ID = 1;
                while (!reader.EndOfStream)
                {
                    //докато достигнем края на stream-a, четем ред по ред и записваме всеки ред в стринг - line, след което ако не е празен ред или null(EOF), разделяме стринга там където се среща запетая
                    string line = reader.ReadLine();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        //string[] values играе ролята на всеки запис от реда
                        string[] values = line.Split(',');
                        string toAdd = ID.ToString();
                        for (int i = 0; i < values.Length; i++)
                        {
                            toAdd += ", '" + values[i];
                            if (i + 1 <= values.Length)
                            {
                                toAdd += "'";
                            }
                        }
                        row.Add(toAdd);
                        //row.Add(personID + ", '" + values[0] + "', '" + values[1] + "', '" + values[2] + "', " + values[3]);
                        try
                        {
                            sql = "INSERT INTO dbo." + fileNameOnly[0] + " (ID, " + columnNamesOnly + ") VALUES (" + row[ID - 1] + ");";
                            MessageBox.Show("Command to be executed:\n" + sql);
                            command = new SqlCommand(sql, connection);
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error: " + ex);
                            connection.Close();
                        }
                    }
                    ID++;
                }
                connection.Close();
                //тук просто приготвяме съдържанието на messagebox-a със съдържанието от прочетения файл - пак казвам - чисто информативно е това
                string[] rowArray = row.ToArray();
                string AllFilesMessage = "";
                foreach (string file in rowArray)
                    AllFilesMessage += file + "\n";
                MessageBox.Show("Inserted from file(" + safeFileName + "):\n\n" + AllFilesMessage);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error: " + exc);
                connection.Close();
            }
        }
    }
}