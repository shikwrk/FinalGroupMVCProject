﻿const TotalPage = document.getElementById('course-total-page');
const totalClasses = document.getElementById('totalClasses');
//載入課程資料
const FilterSortData = {
    page: 1,
    pageSize: 6,
    keyword: '',
    fieldId: undefined,
    subjectName: undefined,
    minPrice: 100,
    maxPrice: 2000,
    sortBy: 'desc',
    sortType: 'newest'
};

const handleSubjects = (filterData) => {
    FilterSortData.fieldId = filterData.fieldId;
    FilterSortData.subjectName = filterData.subjectName;
    FilterSortData.page = filterData.page;
    FilterSortData.pageSize = filterData.pageSize;
    loadCourses(FilterSortData);
}
const handleAllCourses = () => {  
    FilterSortData.fieldId = undefined;
    FilterSortData.subjectName = undefined; 
    loadCourses(FilterSortData);
}
const handlePriceInput = () => {
    // 獲取最低價格和最高價格
    const minPrice = document.getElementById('min').value;
    const maxPrice = document.getElementById('max').value;

    // 更新篩選條件並重新加載課程列表
    FilterSortData.minPrice = minPrice;
    FilterSortData.maxPrice = maxPrice;
    loadCourses(FilterSortData);
}

const handleSorting = (value) => {
    if (value === 'newest') {
        FilterSortData.sortType = 'newest';
        FilterSortData.page = 1;
    } else if (value === 'PriceDesc') {
        FilterSortData.sortType = 'PriceDesc';
        FilterSortData.page = 1;
    } else if (value === 'PriceAsc') {
        FilterSortData.sortType = 'PriceAsc';
        FilterSortData.page = 1;
    } else if (value == 'old') {
        FilterSortData.sortType = 'old';
        FilterSortData.page = 1;
    }

    // 調用 loadCourses 函數重新加載課程列表
    loadCourses(FilterSortData);
}

const loadCourses = async (FilterSortData) => {
    const { page = 1, pageSize = 6, keyword, fieldId, subjectName, minPrice = 100, maxPrice = 2000, sortBy, sortType } = FilterSortData;
    // 生成url 的工具
    const urlParams = new URLSearchParams();
    urlParams.append('Page', page);
    urlParams.append('PageSize', pageSize);
    if (keyword) urlParams.append('keyword', keyword);
    if (fieldId) urlParams.append('FieldId', fieldId);    
    if (subjectName) urlParams.append('subjectName', subjectName);
    if (minPrice) urlParams.append('minPrice', minPrice);
    if (maxPrice) urlParams.append('maxPrice', maxPrice);
    if (sortBy) urlParams.append('sortBy', sortBy);
    if (sortType) urlParams.append('sortType', sortType);
    
    let url = `/Lesson/CourseList?${urlParams.toString()}`;
    console.log(url)
    
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error('Network response was not ok');
    }
    const data = await response.json();
    console.log(data)
    addPaging(data)  
    createCourseHtml(data)
    totalClasses.textContent = data.totalCount
    
    
};

const addPaging = (data) => {
    TotalPage.innerHTML = '';
    for (let i = 1; i <= data.totalPages; i++) {
        const itemHTML = `<li class="page-item"><button class="page-link" onclick="pagingHandler(${i})">${i}</button></li>`;
        TotalPage.insertAdjacentHTML('beforeend', itemHTML);
    }   
}
// 處理分頁
const pagingHandler = page => {
    FilterSortData.page = page
    loadCourses(FilterSortData);   
}

const createCourseHtml = data => {
    const createCourseHtml = document.getElementById('createCourseHtml');
    createCourseHtml.innerHTML = '';
    for (const item of data.courses) {
       
        let html = ''
        // 使用Intl API 轉換時間
        const date = new Date(`${item.lessonCourse.fLessonDate}`);
        const formatter = new Intl.DateTimeFormat('zh-TW', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',           
        });
        const formattedDate = formatter.format(date);
        html = `<div class="col-xl-4 col-sm-6">
    <div class="course-default border radius-md mb-25">
        <figure class="course-img">
            <a href="/Lesson/Details/${item.lessonCourse.fLessonCourseId}" title="" target="_self" class="lazy-container ratio ratio-2-3">
                <img src="data:image/gif;base64,${item.imageData}" onerror="this.src='/images/Clement/nothing.jpg'" alt="/images/Clement/nothing.jpg">                    
            </a>
            <div class="hover-show">
                <a href="/Lesson/Details/${item.lessonCourse.fLessonCourseId}" class="btn btn-md btn-primary rounded-pill" title="Enroll Now" target="_self">立即預約</a>
            </div>
        </figure>
        <div class="course-details">
            <div class="p-3">
                <a href="/Lesson/Details/${item.lessonCourse.fLessonCourseId}" target="_self" title="${item.fieldName}" class="tag font-sm color-primary mb-1">${item.fieldName}/${item.subjectName}</a>
                <h6 class="course-title lc-2 mb-0">
                    <a href="/Lesson/Details/${item.lessonCourse.fLessonCourseId}" target="_self" title="Link">
                        ${item.lessonCourse.fName}
                    </a>
                </h6>
                <div class="authors mt-15">
                    <div class="author">
                         <img src="data:image/gif;base64,${item.teacherImage}" onerror="this.src='/images/Clement/nothing.jpg'" alt="/images/Clement/nothing.jpg">
                        <span class="font-sm">

                            <a href="/Teacher/Info/${item.id}" target="_self" title=" ${item.lessonCourse.teacherName}">
                                 ${item.teacherName}
                            </a>
                        </span>
                         
                    </div>   
                    
                </div>
            </div>
            <div class="course-bottom-info px-3 py-2">                
                <span class="font-sm"><i class="fas fa-usd-circle"></i>NT ${item.lessonCourse.fPrice}</span>  
               <span class="font-sm icon-start"><i class="fa-solid fa-calendar-days color-primary"></i>${formattedDate}</span>
            </div>
            
        </div>
    </div>
</div>`
        createCourseHtml.insertAdjacentHTML('beforeend', html)
    }

}

// 頁面載入時，順便載入JS file
loadCourses(FilterSortData);

// 模板的bug 暫時棄用，可能會用到程式碼，留著考古
//const createFilterHtml = (data) => {
//    const fieldsSubjects = document.getElementById('fieldsSubjects');
//    for (const item of data.fieldWithSubjects) {
//        const { fieldId, fieldName, subjectNames } = item;
//        let subjectHTML = ''; // 初始化空的 HTML 字符串
//        // 使用 map 方法遍歷 subjectNames 陣列並生成對應的 HTML
//        subjectHTML = subjectNames.map(subjectName => {
//            return `<li><a class="icon-start" href="courses.html" title="link" target="_self"><i class="fal fa-angle-right"></i>${subjectName}<span class="qty"></span></a></li>`;
//        }).join(''); // 將生成的 HTML 字符串使用 join 方法合併成一個字符串
//        const itemHTML = `<li class="list-item list-dropdown">
//            <a class="category-toggle" href="#${fieldId}">${fieldName}<span class="qty"></span></a>
//            <ul class="menu-collapse">
//                ${subjectHTML}
//            </ul>
//        </li>`;
//        fieldsSubjects.insertAdjacentHTML('beforeend', itemHTML);
//        console.log(itemHTML)

//    }
//}

// 分頁預設
