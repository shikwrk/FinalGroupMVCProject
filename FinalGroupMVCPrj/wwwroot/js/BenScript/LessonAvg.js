document.addEventListener("DOMContentLoaded", async function () {
    /*    const lessonCourseId = @Model.FLessonCourseId;*/
    let element = document.querySelector('div[FLessonCourseId]');
    let lessonCourseId = element.getAttribute('FLessonCourseId');

    const url = "/LessonReview/GetAvgEvalScore?CourseId=" + lessonCourseId;

    try {
        const response = await fetch(url, {
            method: "GET"
        });

        if (!response.ok) {
            throw new Error("Network response was not ok");
        }

        const data = await response.text();
        document.getElementById("avgevalpartial").innerHTML = data;
    } catch (error) {
        console.error("There was a problem with the fetch operation:", error);
    }
});