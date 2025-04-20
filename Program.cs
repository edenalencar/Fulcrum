using System;

class Program
{
    static bool ContinuarIteracao()
    {
        Console.WriteLine("\nDeseja continuar iterando? (S/N):");
        string resposta = Console.ReadLine().ToUpper();
        return resposta == "S";
    }

    static void Main(string[] args)
    {
        do
        {
            // Coloque aqui o código principal do loop
            Console.WriteLine("Iniciando iteração...");
            
            // Lógica da iteração aqui
            
        } while (ContinuarIteracao());
        
        Console.WriteLine("Programa finalizado.");
        Console.ReadKey();
    }
}