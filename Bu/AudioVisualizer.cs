    // Método auxiliar que verifica de forma segura se um AudioFileReader está em estado válido
    private bool IsSafeToUse(AudioFileReader reader)
    {
        // Verificação mais rápida logo no início
        if (reader == null || _isDisposed)
            return false;
        
        // Proteção adicional para garantir que não estamos tentando acessar um objeto já descartado
        if (reader.GetType().GetProperty("Position") == null)
            return false;
            
        // Lista de verificações encadeadas, cada uma em um bloco try/catch independente
        // para evitar que uma falha em uma propriedade cause exceção não tratada
        
        // 1. Verificar WaveFormat (geralmente mais estável)
        try 
        {
            // Isso será null se o AudioFileReader já foi descartado
            var waveFormat = reader.WaveFormat;
            if (waveFormat == null) 
                return false;
        }
        catch
        {
            return false;
        }
        
        // 2. Acesso ao Position com proteção máxima
        try
        {
            long position;
            
            try 
            {
                // Captura qualquer exceção que possa ser lançada ao acessar Position
                position = reader.Position;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            catch (ObjectDisposedException) 
            {
                return false;
            }
            catch
            {
                return false;
            }
            
            if (position < 0) 
                return false;
        }
        catch
        {
            return false;
        }
        
        // 3. Acesso ao Length com proteção máxima
        try
        {
            long length;
            
            try 
            {
                length = reader.Length;
            }
            catch
            {
                return false;
            }
            
            if (length <= 0) 
                return false;
        }
        catch
        {
            return false;
        }
        
        // Todas as verificações passaram
        return true;
    }