using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureGIS_Geo_QC.Exports.Models;

namespace PureGIS_Geo_QC.Models
{
    internal class DataModels
    {
    }
    
    // 프로젝트 최상위 클래스
    public class ProjectDefinition
    {
        public string ProjectName { get; set; }        // 지역명/기관명
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public List<InfrastructureCategory> Categories { get; set; } = new List<InfrastructureCategory>();
        public List<CodeSet> CodeSets { get; set; } = new List<CodeSet>(); // 코드셋 리스트 추가
    }

    // 8대 지하시설물 분류
    public class InfrastructureCategory
    {
        public string CategoryId { get; set; }     // ROAD, WATER, SEWER, ELECTRIC, TELECOM, HEAT, GAS, OIL
        public string CategoryName { get; set; }   // 도로, 상수, 하수, 전기, 통신, 열배관, 가스, 송유
        public List<TableDefinition> Tables { get; set; } = new List<TableDefinition>();
    }

    // 8대 시설물 기본 목록 제공용
    public static class InfrastructureTypes
    {
        public static readonly Dictionary<string, string> DefaultCategories = new Dictionary<string, string>
    {
        {"ROAD", "도로"},
        {"WATER", "상수"},
        {"SEWER", "하수"},
        {"ELECTRIC", "전기"},
        {"TELECOM", "통신"},
        {"HEAT", "열배관"},
        {"GAS", "가스"},
        {"OIL", "송유"}
    };
    }

    // 컬럼의 속성을 정의하는 클래스
    public class ColumnDefinition
    {
        public string ColumnId { get; set; }
        public string ColumnName { get; set; }
        public string Type { get; set; }
        public string Length { get; set; }
        public bool IsNotNull { get; set; }
        public string KeyType { get; set; } // PK/FK
        public string CodeName { get; set; } // 코드명 속성
        public string Remarks { get; set; }
    }

    // 테이블의 속성을 정의하는 클래스
    public class TableDefinition
    {
        public string TableId { get; set; }
        public string TableName { get; set; }
        public BindingList<ColumnDefinition> Columns { get; set; } = new BindingList<ColumnDefinition>();
    }        
    // 검증 결과를 담는 클래스
    public class ColumnValidationResult
    {
        // 전체 상태 (정상/오류)
        public string Status { get; set; }

        // 기준(Standard) 값 - ✅ Std_ColumnId는 이미 있음
        public string Std_ColumnId { get; set; }
        public string Std_ColumnName { get; set; }
        public string Std_Type { get; set; }
        public string Std_Length { get; set; }

        // ❌ 추가 필요한 필드들
        public string Found_FieldName { get; set; }     // 실제 찾은 필드명
        public bool IsFieldFound { get; set; }          // 필드 존재 여부

        // 현재(Current) 값
        public string Cur_Type { get; set; }
        public string Cur_Length { get; set; }

        // 각 항목의 일치 여부
        public bool IsTypeCorrect { get; set; }
        public bool IsLengthCorrect { get; set; }
        public bool IsNotNullCorrect { get; set; } // NOT NULL 검사 결과
        public int NotNullErrorCount { get; set; } = 0; // NOT NULL 위반 개수

        public bool IsCodeCorrect { get; set; } // 코드값 검사 결과
        public int CodeErrorCount { get; set; } = 0; // 코드값 오류 개수
    }

    /// <summary>
    /// 파일의 컬럼 정보를 표시하기 위한 클래스
    /// </summary>
    public class FileColumnInfo
    {
        public string ColumnName { get; set; }
        public TypeInfo DataType { get; set; }
        public string MaxLength { get; set; }
    }

    /// <summary>
    /// 타입 정보를 표시하기 위한 클래스
    /// </summary>
    public class TypeInfo
    {
        public string Name { get; set; }
    }
    // =======================================================
    // ✨ 1. 다중 파일 보고서 데이터를 담는 클래스 (올바른 위치)
    // =======================================================
    public class MultiFileReport
    {
        public string ProjectName { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public List<ReportData> FileResults { get; set; } = new List<ReportData>();

        // 전체 파일에 대한 요약 통계
        public int TotalFiles => FileResults?.Count ?? 0;
        public int TotalColumns => FileResults?.Sum(r => r.TotalCount) ?? 0;
        public int TotalNormalColumns => FileResults?.Sum(r => r.NormalCount) ?? 0;
        public int TotalErrorColumns => FileResults?.Sum(r => r.ErrorCount) ?? 0;
        public string OverallSuccessRate
        {
            get
            {
                if (TotalColumns == 0) return "0%";
                return ((double)TotalNormalColumns / TotalColumns * 100).ToString("F1") + "%";
            }
        }
    }

    // 코드와 코드명을 한 쌍으로 담는 클래스 (신규 추가)
    public class CodeValue
    {
        public string Code { get; set; }        // 예: ADD001
        public string Description { get; set; } // 예: 맨홀
    }

    // 코드 그룹과 코드 리스트를 담는 클래스
    public class CodeSet
    {
        public string CodeName { get; set; } // 예: FTR_CDE

        // ===== 👇 [수정] List를 BindingList로 변경합니다. =====        
        public BindingList<CodeValue> Codes { get; set; } = new BindingList<CodeValue>();
    }
}