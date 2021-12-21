using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Practise.Areas.AdminPanel.Data;
using Practise.DataAccessLayer;
using Practise.Models;

namespace Practise.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _dbContext;

        public CategoryController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _dbContext.Categories.Where(x => x.IsDeleted == false).ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            var parentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain)
                .ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, int parentCategoryId)
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain)
                .ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            if (!ModelState.IsValid)
                return View();

            if (category.IsMain)
            {
                var existCategory = parentCategories.Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existCategory)
                {
                    ModelState.AddModelError("Name", "Bu adda kateqoriya var");
                    return View();
                }

                if (category.Photo == null)
                {
                    ModelState.AddModelError("", "Shekil sechin");
                    return View();
                }

                if (!category.Photo.IsImage())
                {
                    ModelState.AddModelError("", "Zehmet olmasa yalniz shekil sechin");
                    return View();
                }

                if (!category.Photo.IsSizeAllowed(1))
                {
                    ModelState.AddModelError("", "Zehmet olmasa 1 Mb-dan az olan shekil sechin");
                    return View();
                }

                var fileName = await FileUtil.GenerateFile(Constants.ImageFolderPath, category.Photo);

                category.Image = fileName;
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Kateqoriya sechin");
                    return View();
                }

                var parentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => y.IsDeleted == false))
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == parentCategoryId);
                if (parentCategory == null)
                    return NotFound();

                foreach (var child in parentCategory.Children)
                {
                    if (child.Name.ToLower() == category.Name.ToLower())
                    {
                        ModelState.AddModelError("Name", "Bu adda sub category var");
                        return View();
                    }
                }

                //var existChild = parentCategory.Children.Any(x => x.Name.ToLower() == category.Name.ToLower());
                //if (existChild)
                //{
                //    ModelState.AddModelError("Name", "Bu adda sub category var");
                //    return View();
                //}

                category.Parent = parentCategory;
            }

            category.IsDeleted = false;
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Include(x => x.Parent).Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (category == null)
                return NotFound();

            category.IsDeleted = true;
            if (category.IsMain)
            {
                foreach (var child in category.Children)
                {
                    child.IsDeleted = true;
                }
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        //Detail
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
            .Include(x => x.Parent).Include(x => x.Children.Where(y => y.IsDeleted == false))
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (id == null)
                return NotFound();

            return View(category);

        }

        //Update
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
            .Include(x => x.Parent).Include(x => x.Children.Where(y => y.IsDeleted == false))
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (id == null)
                return NotFound(); 

            return View(category);


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Category category)
        {
            if (id == null)
                return NotFound();

            if (id != category.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View();

            var dbCategory = await _dbContext.Categories.FindAsync(id);
            if (dbCategory == null)
                return NotFound();

            var isExist = await _dbContext.Categories.AnyAsync(x => x.Name.ToLower() == category.Name.ToLower() &&
                                                                    x.Id != category.Id);
            if (isExist)
            {
                ModelState.AddModelError("Name", "Bu adda kateqoriya var");
                return View();
            }

            

            category.IsDeleted = false;
            dbCategory.Name = category.Name;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


    }


}

