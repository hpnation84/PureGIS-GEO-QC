using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PureGIS_Geo_QC.Managers
{
    // 서버에서 받아올 버전 정보의 구조를 정의하는 클래스
    public class VersionInfo
    {
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleaseNotes { get; set; } // 업데이트 내용
    }

    public static class UpdateManager
    {
        // 최신 버전 정보가 담긴 JSON 파일이 있는 서버 주소
        private static readonly string VersionInfoUrl = "https://www.jindigo.kr/licensephp/version.json";

        /// <summary>
        /// 현재 실행 중인 프로그램의 버전을 가져옵니다.
        /// </summary>
        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// 서버에서 최신 버전 정보를 비동기적으로 가져옵니다.
        /// </summary>
        public static async Task<VersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // 서버에서 version.json 파일을 읽어옵니다.
                    string json = await client.GetStringAsync(VersionInfoUrl);
                    // JSON 텍스트를 VersionInfo 객체로 변환합니다.
                    return JsonConvert.DeserializeObject<VersionInfo>(json);
                }
            }
            catch (Exception ex)
            {
                // 네트워크 오류 등 예외 발생 시 null 반환
                System.Diagnostics.Debug.WriteLine($"업데이트 확인 오류: {ex.Message}");
                return null;
            }
        }
    }
}