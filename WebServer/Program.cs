namespace WebServer
{
    public class Program
    {
        // �D�J�f�I�A���ε{�Ǳq�o�̶}�l����
        public static void Main(string[] args)
        {
            // 1. �Ыؤ@�� Web ���ε{�Ǻc�ؾ�
            var builder = WebApplication.CreateBuilder(args);

            // 2. �N�A�ȲK�[��e���� (DI Container)
            // �o�̲K�[�F MVC �ݭn������M���Ϥ��
            builder.Services.AddControllersWithViews();

            // 3. �c�����ε{��
            var app = builder.Build();

            // 4. �t�m HTTP �ШD�޹D (Middleware Pipeline)
            // �p�G���O�}�o���ҡA�h�ϥβ��`�B�z�{��
            if (!app.Environment.IsDevelopment())
            {
                // ��o�ͥ��B�z�����`�ɡA���w�V�� /Home/Error
                app.UseExceptionHandler("/Home/Error");
                // �ҥ� HSTS (HTTP Strict Transport Security)
                app.UseHsts();
            }

            // �ҥ� HTTPS ���w�V (�j��N http �ର https)
            app.UseHttpsRedirection();

            // �ҥθ��ѥ\��
            app.UseRouting();

            // �ҥα��v�\��
            app.UseAuthorization();

            // �M�g�R�A�귽
            app.MapStaticAssets();

            // �]�w������ѳW�h
            app.MapControllerRoute(
                name: "default", // ���ѦW��
                pattern: "{controller=Home}/{action=Index}/{id?}"); // ���ѼҦ�
                                                                    // �w�] Controller = Home
                                                                    // �w�] Action = Index
                                                                    // id? = id �O�i��Ѽ�

            // �B�����ε{��
            app.Run();
        }
    }
}
