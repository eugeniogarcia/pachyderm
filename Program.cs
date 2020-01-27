using System;
using System.IO;


namespace clasifica
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                if (args.Length < 3)
                {
                    Console.WriteLine("Argumentos incorrectos");
                    Console.WriteLine("clasifica [operacion] [directorio entrada] [directorio de salida]");
                    Console.WriteLine("Con operacion tomando el valor 1, 2, 3 o 4");
                    System.Environment.Exit(-1);
                }
                StreamWriter salida;
                switch (args[0])
                {
                    case "1":
                        Console.WriteLine("Edades");
                        foreach (String archivo in Directory.GetFiles(args[1]))
                        {
                            salida = File.CreateText(args[2] + "/Edad" + Path.GetFileName(archivo));
                            procesaEdades(archivo, salida);
                            salida.Close();
                        }
                        break;
                    case "2":
                        Console.WriteLine("Edades Agregado");
                        salida = File.CreateText(args[2] + "/EdadPersonas" + System.DateTime.Now.Ticks.ToString() + ".txt");
                        foreach (String archivo in Directory.GetFiles(args[1]))
                        {
                            procesaEdades(archivo, salida);
                        }
                        salida.Close();
                        break;
                    case "3":
                        Console.WriteLine("Profesion");
                        foreach (String archivo in Directory.GetFiles(args[1]))
                        {
                            salida = File.CreateText(args[2] + "/Profesion" + Path.GetFileName(archivo));
                            procesaProfesion(archivo, salida);
                            salida.Close();
                        }
                        break;
                    case "4":
                        Console.WriteLine("Profesion Agregado");
                        salida = File.CreateText(args[2] + "/ProfesionPersonas" + System.DateTime.Now.Ticks.ToString() + ".txt");
                        foreach (String archivo in Directory.GetFiles(args[1]))
                        {
                            procesaProfesion(archivo, salida);
                        }
                        salida.Close();
                        break;
                    case "5":
                        Console.WriteLine("Listado");
                        if (args.Length < 4)
                        {
                            Console.WriteLine("Argumentos incorrectos");
                            Console.WriteLine("clasifica 5 [directorio entrada 1] [directorio entrada 2] [directorio de salida]"); System.Environment.Exit(-1);
                        }
                        salida = File.CreateText(args[3] + "/Listado" + System.DateTime.Now.Ticks.ToString() + ".txt");
                        foreach (String archivo in Directory.GetFiles(args[1]))
                        {
                            salida.WriteLine(archivo);
                        }
                        foreach (String archivo in Directory.GetFiles(args[2]))
                        {
                            salida.WriteLine(archivo);
                        }
                        salida.Close();
                        break;
                    default:
                        Console.WriteLine("Operacion incorrecta. Tiene que ser 1, 2, 3, 4 o 5");
                        System.Environment.Exit(-2);
                        break;
                }
            }
            catch(System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                System.Environment.Exit(-3);
            }
        }
            
        static void procesaEdades(String archivo,StreamWriter salida)
        {
            foreach (String linea in File.ReadAllLines(archivo))
            {
                string[] valores = linea.Split(";");
                int edad = 2020 - int.Parse(valores[4]) - 1;
                salida.WriteLine(linea + ";" + edad.ToString());
            }
        }
        static void procesaEdades(String directorio)
        {
            foreach (String archivo in Directory.GetFiles(directorio))
            {
                StreamWriter salida = File.CreateText(directorio + "/ConEdad" + Path.GetFileName(archivo));
                procesaEdades(archivo, salida);
                salida.Close();
            }
        }

        static void procesaProfesion(String archivo, StreamWriter salida)
        {
            foreach (String linea in File.ReadAllLines(archivo))
            {
                string[] valores = linea.Split(";");
                string profesion = "trabajador";
                if (int.Parse(valores[7]) < 20){
                    profesion = "Instituto";
                }
                if (int.Parse(valores[7]) < 13)
                {
                    profesion = "ESO";
                }
                salida.WriteLine(valores[0]+";" + valores[1] + ";" + valores[2] + ";" + profesion);
            }
        }
        static void procesaProfesion(String directorio)
        {
            foreach (String archivo in Directory.GetFiles(directorio))
            {
                StreamWriter salida = File.CreateText(directorio + "/Profesion" + Path.GetFileName(archivo));
                procesaProfesion(archivo, salida);
                salida.Close();
            }
        }
    }
}
